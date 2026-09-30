using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Services.Traitements;

/// <summary>
/// Historique d'un traitement de l'utilisatrice connectée, un mois à la fois : prises prévues, faites ou ignorées,
/// prises au besoin ou séances, et le même compte le mois précédent. Lecture seule.
/// </summary>
public class HistoriqueTraitementsService(AppDbContext context, TraitementsService traitements)
{
    public async Task<ResultatOperation<HistoriqueTraitementViewModel>> GetAsync(string userId, int id, DateOnly mois, DateOnly jour, CancellationToken ct)
    {
        if (await traitements.CarnetDeAsync(userId, ct) is not { } carnetId)
            return ResultatOperation<HistoriqueTraitementViewModel>.Echec(StatutOperation.NonAuthentifie);
        var traitement = await context.Medicaments.AsNoTracking().Include(m => m.Horaires).SingleOrDefaultAsync(m => m.Id == id, ct);
        if (traitement == null) return ResultatOperation<HistoriqueTraitementViewModel>.Echec(StatutOperation.Introuvable);
        if (traitement.CarnetSanteId != carnetId) return ResultatOperation<HistoriqueTraitementViewModel>.Echec(StatutOperation.Interdit);

        if (mois.Year < 2000 || mois.Year > 2100 || jour.Year < 2000 || jour.Year > 2100)
            return ResultatOperation<HistoriqueTraitementViewModel>.Echec(StatutOperation.Invalide, "Mois ou jour invalide.");

        var debut = new DateOnly(mois.Year, mois.Month, 1);
        var fin = debut.AddMonths(1);
        var entrees = await EntreesAsync(traitement, debut.AddMonths(-1), fin, ct);
        var duMois = entrees.Where(e => DateOnly.FromDateTime(e.Date) >= debut).ToList();

        return ResultatOperation<HistoriqueTraitementViewModel>.Succes(new HistoriqueTraitementViewModel
        {
            Traitement = TraitementsService.Vue(traitement),
            Prevues = PrisesPrevues(traitement, debut, fin.AddDays(-1) < jour ? fin.AddDays(-1) : jour),
            Faites = duMois.Count(e => e.Nature != "Ignore"),
            Ignorees = duMois.Count(e => e.Nature == "Ignore"),
            FaitesMoisPrecedent = entrees.Count(e => DateOnly.FromDateTime(e.Date) < debut && e.Nature != "Ignore"),
            Jours = duMois
                .GroupBy(e => DateOnly.FromDateTime(e.Date))
                .OrderByDescending(g => g.Key)
                .Select(g => new JourHistoriqueTraitementViewModel
                {
                    Jour = g.Key.ToString("yyyy-MM-dd"),
                    Entrees = g.OrderByDescending(e => e.Date).ToList(),
                })
                .ToList(),
        });
    }

    /// <summary>Prises ou séances du traitement entre deux jours (fin exclue), une seule lecture pour deux mois.</summary>
    private async Task<List<EntreeHistoriqueTraitementViewModel>> EntreesAsync(Medicament traitement, DateOnly debut, DateOnly fin, CancellationToken ct)
    {
        var de = debut.ToDateTime(TimeOnly.MinValue);
        var a = fin.ToDateTime(TimeOnly.MinValue);
        if (traitement.Type == TypeTraitement.NonMedicamenteux)
        {
            return await context.DonneesTraitementNonMedicamenteux.AsNoTracking()
                .Where(s => s.MedicamentId == traitement.Id && s.CarnetSanteId == traitement.CarnetSanteId && s.Date >= de && s.Date < a)
                .Select(s => new EntreeHistoriqueTraitementViewModel { Id = s.Id, Nature = "Seance", Date = s.Date })
                .ToListAsync(ct);
        }

        var prises = await context.DonneesMedicaments.AsNoTracking()
            .Where(p => p.MedicamentId == traitement.Id && p.CarnetSanteId == traitement.CarnetSanteId && p.Date >= de && p.Date < a)
            .ToListAsync(ct);
        return prises.Select(p => new EntreeHistoriqueTraitementViewModel
        {
            Id = p.Id,
            Nature = p.Statut == StatutPrise.Ignore ? "Ignore" : "Pris",
            Date = p.Date,
            HeurePrevue = p.HeurePrevue?.ToString("HH:mm"),
        }).ToList();
    }

    /// <summary>Prises prévues du premier jour au dernier (inclus), dans les dates du traitement.</summary>
    private static int PrisesPrevues(Medicament traitement, DateOnly premier, DateOnly dernier)
    {
        var total = 0;
        for (var jour = premier; jour <= dernier; jour = jour.AddDays(1))
        {
            if (PlanningTraitement.EstPrevuDansSesDates(traitement, jour)) total += traitement.Horaires.Count;
        }
        return total;
    }
}
