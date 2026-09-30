using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Services.Activite;

/// <summary>
/// Activités physiques de l'utilisatrice connectée (carnet déduit de la session) : un mois à la fois, saisie,
/// modification et suppression. Toute entité est chargée en base et son carnet vérifié.
/// </summary>
public class ActiviteService(AppDbContext context, CarnetSanteService carnetSanteService, TimeProvider horloge)
{
    public async Task<ResultatOperation<List<ActiviteViewModel>>> GetMoisAsync(string userId, DateOnly mois, CancellationToken ct)
    {
        if (await CarnetDeAsync(userId, ct) is not { } carnetId) return ResultatOperation<List<ActiviteViewModel>>.Echec(StatutOperation.NonAuthentifie);

        var debut = new DateTime(mois.Year, mois.Month, 1);
        var activites = await context.DonneesActivitePhysique.AsNoTracking()
            .Where(a => a.CarnetSanteId == carnetId && a.Date >= debut && a.Date < debut.AddMonths(1))
            .OrderByDescending(a => a.Date)
            .ToListAsync(ct);
        return ResultatOperation<List<ActiviteViewModel>>.Succes(activites.Select(Vue).ToList());
    }

    public async Task<ResultatOperation<int>> CreerAsync(string userId, ActiviteDto dto, CancellationToken ct)
    {
        if (await CarnetDeAsync(userId, ct) is not { } carnetId) return ResultatOperation<int>.Echec(StatutOperation.NonAuthentifie);
        if (Erreur(dto) is { } erreur) return ResultatOperation<int>.Echec(StatutOperation.Invalide, erreur);

        var activite = new DonneesActivitePhysique { CarnetSanteId = carnetId };
        Appliquer(activite, dto);
        context.DonneesActivitePhysique.Add(activite);
        await context.SaveChangesAsync(ct);
        carnetSanteService.InvalidateCache(carnetId);
        return ResultatOperation<int>.Succes(activite.Id);
    }

    public async Task<ResultatOperation> ModifierAsync(string userId, int id, ActiviteDto dto, CancellationToken ct)
    {
        var (activite, echec) = await ActiviteDeAsync(userId, id, ct);
        if (echec != null) return echec;
        if (Erreur(dto) is { } erreur) return ResultatOperation.Echec(StatutOperation.Invalide, erreur);

        Appliquer(activite!, dto);
        await context.SaveChangesAsync(ct);
        carnetSanteService.InvalidateCache(activite!.CarnetSanteId);
        return ResultatOperation.Succes();
    }

    public async Task<ResultatOperation> SupprimerAsync(string userId, int id, CancellationToken ct)
    {
        var (activite, echec) = await ActiviteDeAsync(userId, id, ct);
        if (echec != null) return echec;

        context.DonneesActivitePhysique.Remove(activite!);
        await context.SaveChangesAsync(ct);
        carnetSanteService.InvalidateCache(activite!.CarnetSanteId);
        return ResultatOperation.Succes();
    }

    private string? Erreur(ActiviteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Type)) return "Le type d'activité est obligatoire.";
        if (!Enum.IsDefined(dto.Niveau)) return "Intensité inconnue.";
        if (!Enum.IsDefined(dto.Effet)) return "Effet sur la douleur inconnu.";
        var demain = DateOnly.FromDateTime(horloge.GetUtcNow().UtcDateTime).AddDays(1);
        return DateOnly.FromDateTime(dto.Date) > demain ? "Une activité à venir ne peut pas être notée." : null;
    }

    private static void Appliquer(DonneesActivitePhysique activite, ActiviteDto dto)
    {
        activite.TypeActivite = dto.Type.Trim();
        activite.Date = dto.Date;
        activite.Duree = dto.Duree;
        activite.NiveauIntensite = (int)dto.Niveau;
        // Ancienne échelle conservée pour une image antérieure et l'export PDF.
        activite.Intensite = dto.Niveau switch { NiveauActivite.Douce => 2, NiveauActivite.Moderee => 5, _ => 8 };
        activite.EffetDouleur = (int)dto.Effet;
        activite.Commentaire = string.IsNullOrWhiteSpace(dto.Commentaire) ? null : dto.Commentaire.Trim();
    }

    private static ActiviteViewModel Vue(DonneesActivitePhysique a) => new()
    {
        Id = a.Id,
        Type = a.TypeActivite,
        Date = a.Date.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
        Duree = a.Duree,
        // Une activité antérieure à la migration sans niveau garde sa correspondance avec l'ancienne échelle.
        Niveau = (NiveauActivite)NiveauDe(a),
        Effet = Enum.IsDefined((EffetActivite)a.EffetDouleur) ? (EffetActivite)a.EffetDouleur : EffetActivite.NonRenseigne,
        Commentaire = a.Commentaire,
    };

    /// <summary>Niveau d'une séance (1 à 3), y compris pour une séance antérieure à la migration, sans niveau enregistré.</summary>
    internal static int NiveauDe(DonneesActivitePhysique a) => a.NiveauIntensite ?? NiveauDeLAncienneEchelle(a.Intensite);

    /// <summary>Correspondance de la migration : 1-3 douce, 4-7 modérée, 8-10 soutenue.</summary>
    private static int NiveauDeLAncienneEchelle(int intensite)
    {
        if (intensite <= 3) return 1;
        return intensite <= 7 ? 2 : 3;
    }

    private async Task<(DonneesActivitePhysique? Activite, ResultatOperation? Echec)> ActiviteDeAsync(string userId, int id, CancellationToken ct)
    {
        if (await CarnetDeAsync(userId, ct) is not { } carnetId) return (null, ResultatOperation.Echec(StatutOperation.NonAuthentifie));
        var activite = await context.DonneesActivitePhysique.SingleOrDefaultAsync(a => a.Id == id, ct);
        if (activite == null) return (null, ResultatOperation.Echec(StatutOperation.Introuvable));
        return activite.CarnetSanteId != carnetId ? (null, ResultatOperation.Echec(StatutOperation.Interdit)) : (activite, null);
    }

    private async Task<int?> CarnetDeAsync(string userId, CancellationToken ct) => string.IsNullOrEmpty(userId)
        ? null
        : await context.CarnetSantes.Where(c => c.UserId == userId).Select(c => (int?)c.Id).SingleOrDefaultAsync(ct);
}
