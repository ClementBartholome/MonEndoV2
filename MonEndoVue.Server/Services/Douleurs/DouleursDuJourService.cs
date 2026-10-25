using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Services.Douleurs;

/// <summary>
/// Zones de douleur notées dans le bilan d'un jour (« Où as-tu eu mal ? ») : des entrées de douleur marquées
/// <see cref="DonneesDouleur.DepuisBilan"/>, rattachées au bilan de ce jour (remplacées avec lui, supprimées avec lui). Les crises
/// notées sur la page Douleurs ne sont jamais modifiées ici. Carnet déduit de la session.
/// </summary>
public class DouleursDuJourService(AppDbContext context, TimeProvider horloge)
{
    /// <summary>Autant que de types proposés à l'écran.</summary>
    public const int ZonesMaximum = 8;
    public const int TypeLongueurMaximale = 50;

    /// <summary>Une zone est enregistrée à midi du jour : elle n'a pas d'heure (les crises en ont une).</summary>
    private static readonly TimeOnly HeureDesZones = new(12, 0);

    public async Task<ResultatOperation<DouleursDuJourViewModel>> GetAsync(string userId, DateOnly jour, CancellationToken ct)
    {
        if (await CarnetAsync(userId, ct) is not { } carnetId)
            return ResultatOperation<DouleursDuJourViewModel>.Echec(StatutOperation.NonAuthentifie);
        if (!DateSaisieValidator.EstValide(jour.ToDateTime(TimeOnly.MinValue), horloge.GetUtcNow()))
            return ResultatOperation<DouleursDuJourViewModel>.Echec(StatutOperation.Invalide, DateSaisieValidator.Message);

        var entrees = await EntreesDuJour(carnetId, jour).AsNoTracking().OrderBy(d => d.Date).ToListAsync(ct);
        return ResultatOperation<DouleursDuJourViewModel>.Succes(new DouleursDuJourViewModel
        {
            Zones = entrees.Where(d => d.DepuisBilan)
                .Select(d => new ZoneDouleurViewModel { TypeDouleur = d.TypeDouleur, Intensite = d.Intensite }).ToList(),
            Crises = entrees.Where(d => !d.DepuisBilan)
                .Select(d => new CriseDouleurViewModel
                {
                    TypeDouleur = d.TypeDouleur,
                    Intensite = d.Intensite,
                    Heure = d.Date.ToString("HH:mm", CultureInfo.InvariantCulture),
                }).ToList(),
        });
    }

    /// <summary>Remplace les zones du bilan de <paramref name="jour"/> ; le bilan de ce jour doit exister.</summary>
    public async Task<ResultatOperation> EnregistrerZonesAsync(string userId, DateOnly jour, ZonesDouleurBilanDto dto, CancellationToken ct)
    {
        if (await CarnetAsync(userId, ct) is not { } carnetId) return ResultatOperation.Echec(StatutOperation.NonAuthentifie);
        if (!DateSaisieValidator.EstValide(jour.ToDateTime(TimeOnly.MinValue), horloge.GetUtcNow()))
            return ResultatOperation.Echec(StatutOperation.Invalide, DateSaisieValidator.Message);
        if (Valider(dto.Zones) is { } erreur) return ResultatOperation.Echec(StatutOperation.Invalide, erreur);

        var debut = jour.ToDateTime(TimeOnly.MinValue);
        var fin = jour.AddDays(1).ToDateTime(TimeOnly.MinValue);
        if (!await context.BilansQuotidiens.AnyAsync(b => b.CarnetSanteId == carnetId && b.Date >= debut && b.Date < fin, ct))
            return ResultatOperation.Echec(StatutOperation.Introuvable);

        context.DonneesDouleurs.RemoveRange(await EntreesDuJour(carnetId, jour).Where(d => d.DepuisBilan).ToListAsync(ct));
        context.DonneesDouleurs.AddRange(dto.Zones.Select(z => new DonneesDouleur
        {
            CarnetSanteId = carnetId,
            TypeDouleur = z.TypeDouleur.Trim(),
            Intensite = z.Intensite,
            Date = jour.ToDateTime(HeureDesZones),
            DepuisBilan = true,
        }));
        await context.SaveChangesAsync(ct);
        return ResultatOperation.Succes();
    }

    /// <summary>Le bilan de <paramref name="jour"/> est supprimé : ses zones aussi (sans enregistrer, l'appelant le fait).</summary>
    public async Task RetirerZonesAsync(int carnetId, DateOnly jour, CancellationToken ct) =>
        context.DonneesDouleurs.RemoveRange(await EntreesDuJour(carnetId, jour).Where(d => d.DepuisBilan).ToListAsync(ct));

    /// <summary>Le bilan change de jour : ses zones le suivent (sans enregistrer, l'appelant le fait).</summary>
    public async Task DeplacerZonesAsync(int carnetId, DateOnly de, DateOnly vers, CancellationToken ct)
    {
        foreach (var zone in await EntreesDuJour(carnetId, de).Where(d => d.DepuisBilan).ToListAsync(ct))
            zone.Date = vers.ToDateTime(HeureDesZones);
    }

    private static string? Valider(List<ZoneDouleurDto> zones)
    {
        if (zones.Count > ZonesMaximum) return $"{ZonesMaximum} zones au plus.";
        if (zones.Any(z => string.IsNullOrWhiteSpace(z.TypeDouleur) || z.TypeDouleur.Trim().Length > TypeLongueurMaximale))
            return "Chaque zone a un type.";
        if (zones.Any(z => z.Intensite is < 1 or > 10)) return "L'intensité va de 1 à 10.";
        if (zones.Select(z => z.TypeDouleur.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != zones.Count)
            return "Chaque zone n'est notée qu'une fois.";
        return null;
    }

    private IQueryable<DonneesDouleur> EntreesDuJour(int carnetId, DateOnly jour)
    {
        var debut = jour.ToDateTime(TimeOnly.MinValue);
        var fin = jour.AddDays(1).ToDateTime(TimeOnly.MinValue);
        return context.DonneesDouleurs.Where(d => d.CarnetSanteId == carnetId && d.Date >= debut && d.Date < fin);
    }

    private Task<int?> CarnetAsync(string userId, CancellationToken ct) =>
        context.CarnetSantes.Where(c => c.UserId == userId).Select(c => (int?)c.Id).SingleOrDefaultAsync(ct);
}
