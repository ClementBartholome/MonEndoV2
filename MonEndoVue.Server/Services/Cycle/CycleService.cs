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

    /// <summary>Cycles comptés dans la moyenne : les plus récents, pour refléter la période actuelle.</summary>
    public const int CyclesDeLaMoyenne = 6;

    /// <summary>Plafond d'une demande d'historique (« Voir plus ») : la réponse reste légère quelle que soit l'ancienneté.</summary>
    public const int CyclesMaximum = 120;

    public async Task<ResultatOperation<CycleViewModel>> GetAsync(string userId, DateOnly jour, DateOnly mois, CancellationToken ct, int nombreCycles = CyclesDeLaMoyenne)
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

        var tous = HistoriqueCycles.Cycles(HistoriqueCycles.Regrouper(jours), int.MaxValue);
        var cycles = tous.Take(Math.Clamp(nombreCycles, 1, CyclesMaximum)).ToList();
        var recents = tous.Take(CyclesDeLaMoyenne).ToList();
        var enCours = CycleDuJour.Calculer(jours, jour);
        var douleursFortes = await DouleursFortesAsync(carnetId, cycles, ct);

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
            Cycles = cycles.Select(c => new CycleTermineViewModel
            {
                Debut = Texte(c.Debut),
                JoursDeRegles = c.JoursDeRegles,
                Duree = c.Duree,
                JoursDouleurForte = Enumerable.Range(0, c.Duree).Where(i => douleursFortes.Contains(c.Debut.AddDays(i))).Select(i => i + 1).ToList(),
            }).ToList(),
            DureeMoyenne = HistoriqueCycles.DureeMoyenne(recents),
            ReglesMoyenne = HistoriqueCycles.ReglesMoyenne(recents),
            DureeMinimale = recents.Count < 2 ? null : recents.Min(c => c.Duree),
            DureeMaximale = recents.Count < 2 ? null : recents.Max(c => c.Duree),
            CyclesPlusAnciens = tous.Count - cycles.Count,
        });
    }

    /// <summary>Même seuil que les observations de l'onglet Tendances du bilan.</summary>
    public const int SeuilDouleurForte = 6;

    /// <summary>
    /// Jours de douleur forte sur la période des cycles listés : une douleur notée (page Douleurs) ou une douleur moyenne
    /// du bilan quotidien de 6/10 ou plus. Deux lectures bornées à la période, quelle que soit l'ancienneté du carnet.
    /// </summary>
    private async Task<HashSet<DateOnly>> DouleursFortesAsync(int carnetId, IReadOnlyList<CycleTermine> cycles, CancellationToken ct)
    {
        if (cycles.Count == 0) return [];
        var debut = cycles[^1].Debut.ToDateTime(TimeOnly.MinValue);
        var fin = cycles[0].Debut.AddDays(cycles[0].Duree).ToDateTime(TimeOnly.MinValue);

        var douleurs = await context.DonneesDouleurs.AsNoTracking()
            .Where(d => d.CarnetSanteId == carnetId && d.Intensite >= SeuilDouleurForte && d.Date >= debut && d.Date < fin)
            .Select(d => d.Date)
            .ToListAsync(ct);
        var bilans = await context.BilansQuotidiens.AsNoTracking()
            .Where(b => b.CarnetSanteId == carnetId && b.DouleurMoyenne >= SeuilDouleurForte && b.Date >= debut && b.Date < fin)
            .Select(b => b.Date)
            .ToListAsync(ct);
        return douleurs.Concat(bilans).Select(DateOnly.FromDateTime).ToHashSet();
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
