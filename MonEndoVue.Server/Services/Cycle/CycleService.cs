using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services.Accueil;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Services.Cycle;

/// <summary>
/// Règles de l'utilisatrice connectée (carnet déduit de la session) : mois affiché, cycle en cours, historique des
/// cycles, et jours de règles ajoutés ou retirés un par un. Aucune prédiction.
/// </summary>
public class CycleService(AppDbContext context, CarnetSanteService carnetSanteService, TimeProvider horloge)
{
    /// <summary>Écart toléré entre le jour local envoyé par le client et la date du serveur (fuseaux horaires).</summary>
    private const int EcartMaximalJours = 2;

    /// <summary>Nombre de cycles terminés listés (et comptés dans la moyenne).</summary>
    private const int NombreDeCycles = 6;

    public async Task<ResultatOperation<CycleViewModel>> GetAsync(string userId, DateOnly jour, DateOnly mois, CancellationToken ct)
    {
        if (await CarnetDeAsync(userId, ct) is not { } carnetId) return ResultatOperation<CycleViewModel>.Echec(StatutOperation.NonAuthentifie);
        if (!EstAujourdhui(jour)) return ResultatOperation<CycleViewModel>.Echec(StatutOperation.Invalide, "Le jour demandé doit être aujourd'hui.");

        // L'historique entier est court (quelques jours par mois) : une seule lecture sert au mois, au cycle et aux cycles.
        var jours = (await context.JourRegles
                .Where(j => j.CarnetSanteId == carnetId)
                .Select(j => j.Date)
                .ToListAsync(ct))
            .Select(DateOnly.FromDateTime)
            .Distinct()
            .ToList();

        var cycles = HistoriqueCycles.Cycles(HistoriqueCycles.Regrouper(jours), NombreDeCycles);
        var enCours = CycleDuJour.Calculer(jours, jour);

        return ResultatOperation<CycleViewModel>.Succes(new CycleViewModel
        {
            JoursDeRegles = jours
                .Where(j => j.Year == mois.Year && j.Month == mois.Month)
                .Order()
                .Select(Texte)
                .ToList(),
            EnCours = enCours.JourDuCycle is { } jourDuCycle
                ? new CycleEnCoursViewModel
                {
                    Debut = Texte(jour.AddDays(1 - jourDuCycle)),
                    JourDuCycle = jourDuCycle,
                    JourDeRegles = enCours.JourDeRegles,
                }
                : null,
            Cycles = cycles.Select(c => new CycleTermineViewModel { Debut = Texte(c.Debut), JoursDeRegles = c.JoursDeRegles, Duree = c.Duree }).ToList(),
            DureeMoyenne = HistoriqueCycles.DureeMoyenne(cycles),
        });
    }

    /// <summary>Note un jour de règles (sans effet s'il l'est déjà) ; jamais un jour à venir.</summary>
    public async Task<ResultatOperation> AjouterJourAsync(string userId, DateOnly jour, CancellationToken ct)
    {
        if (await CarnetDeAsync(userId, ct) is not { } carnetId) return ResultatOperation.Echec(StatutOperation.NonAuthentifie);
        if (jour.DayNumber > AujourdhuiServeur().DayNumber + 1) return ResultatOperation.Echec(StatutOperation.Invalide, "Un jour à venir ne peut pas être noté.");

        var (debut, fin) = Bornes(jour);
        if (!await context.JourRegles.AnyAsync(j => j.CarnetSanteId == carnetId && j.Date >= debut && j.Date < fin, ct))
        {
            context.JourRegles.Add(new JourRegle { CarnetSanteId = carnetId, Date = debut });
            await context.SaveChangesAsync(ct);
            carnetSanteService.InvalidateCache(carnetId);
        }
        return ResultatOperation.Succes();
    }

    /// <summary>Retire un jour de règles du carnet de la session (sans effet s'il n'était pas noté).</summary>
    public async Task<ResultatOperation> RetirerJourAsync(string userId, DateOnly jour, CancellationToken ct)
    {
        if (await CarnetDeAsync(userId, ct) is not { } carnetId) return ResultatOperation.Echec(StatutOperation.NonAuthentifie);

        var (debut, fin) = Bornes(jour);
        var notes = await context.JourRegles.Where(j => j.CarnetSanteId == carnetId && j.Date >= debut && j.Date < fin).ToListAsync(ct);
        if (notes.Count > 0)
        {
            context.JourRegles.RemoveRange(notes);
            await context.SaveChangesAsync(ct);
            carnetSanteService.InvalidateCache(carnetId);
        }
        return ResultatOperation.Succes();
    }

    private async Task<int?> CarnetDeAsync(string userId, CancellationToken ct) => string.IsNullOrEmpty(userId)
        ? null
        : await context.CarnetSantes.Where(c => c.UserId == userId).Select(c => (int?)c.Id).SingleOrDefaultAsync(ct);

    private DateOnly AujourdhuiServeur() => DateOnly.FromDateTime(horloge.GetUtcNow().UtcDateTime);

    private bool EstAujourdhui(DateOnly jour) => Math.Abs(jour.DayNumber - AujourdhuiServeur().DayNumber) <= EcartMaximalJours;

    /// <summary>Un jour de règles est stocké à minuit, mais d'anciennes saisies portent une heure : on compare par jour.</summary>
    private static (DateTime Debut, DateTime Fin) Bornes(DateOnly jour) =>
        (jour.ToDateTime(TimeOnly.MinValue), jour.AddDays(1).ToDateTime(TimeOnly.MinValue));

    private static string Texte(DateOnly jour) => jour.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}
