using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Services.Accueil;

/// <summary>
/// Accueil « Aujourd'hui » de l'utilisatrice connectée (carnet déduit de la session). Le jour vient du client : les dates
/// sont enregistrées sans fuseau, au jour calendaire local, et le serveur ne connaît pas le fuseau de l'utilisatrice.
/// </summary>
public class AccueilService(AppDbContext context, TimeProvider horloge)
{
    /// <summary>Écart toléré entre le jour demandé et la date du serveur (fuseaux horaires).</summary>
    private const int EcartMaximalJours = 2;

    public async Task<ResultatOperation<AujourdhuiViewModel>> GetAujourdhuiAsync(string userId, DateOnly jour, CancellationToken ct)
    {
        var carnetId = await context.CarnetSantes.Where(c => c.UserId == userId).Select(c => (int?)c.Id).SingleOrDefaultAsync(ct);
        if (carnetId is not { } id) return ResultatOperation<AujourdhuiViewModel>.Echec(StatutOperation.NonAuthentifie);

        var aujourdhuiServeur = DateOnly.FromDateTime(horloge.GetUtcNow().UtcDateTime);
        if (Math.Abs(jour.DayNumber - aujourdhuiServeur.DayNumber) > EcartMaximalJours)
        {
            return ResultatOperation<AujourdhuiViewModel>.Echec(StatutOperation.Invalide, "Le jour demandé doit être aujourd'hui.");
        }

        // Deux semaines de données suffisent à la semaine et à sa comparaison ; le cycle remonte plus loin.
        var debutQuinzaine = jour.AddDays(-13).ToDateTime(TimeOnly.MinValue);
        var finExclue = jour.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var debutCycle = jour.AddDays(-CycleDuJour.DureeMaximaleCycle).ToDateTime(TimeOnly.MinValue);

        var joursDeRegles = (await context.JourRegles.AsNoTracking()
                .Where(j => j.CarnetSanteId == id && j.Date >= debutCycle && j.Date < finExclue)
                .Select(j => j.Date)
                .ToListAsync(ct))
            .Select(DateOnly.FromDateTime)
            .ToHashSet();

        var bilans = await context.BilansQuotidiens.AsNoTracking()
            .Where(b => b.CarnetSanteId == id && b.Date >= debutQuinzaine && b.Date < finExclue)
            .ToListAsync(ct);

        var joursDouleursNotees = (await context.DonneesDouleurs.AsNoTracking()
                .Where(d => d.CarnetSanteId == id && d.Date >= debutQuinzaine && d.Date < finExclue)
                .Select(d => d.Date)
                .ToListAsync(ct))
            .Select(DateOnly.FromDateTime);

        var cycle = CycleDuJour.Calculer(joursDeRegles, jour);
        var bilanDuJour = bilans.FirstOrDefault(b => DateOnly.FromDateTime(b.Date) == jour);

        return ResultatOperation<AujourdhuiViewModel>.Succes(new AujourdhuiViewModel
        {
            Cycle = new CycleAujourdhuiViewModel { EnRegles = cycle.EnRegles, JourDeRegles = cycle.JourDeRegles, JourDuCycle = cycle.JourDuCycle },
            Bilan = bilanDuJour == null ? null : new BilanAujourdhuiViewModel
            {
                DouleurMoyenne = bilanDuJour.DouleurMoyenne,
                Emotions = bilanDuJour.Emotions.Select(e => e.Emotion.ToString()).ToList(),
                Fatigue = bilanDuJour.Fatigue,
            },
            Traitements = await TraitementsDuJourAsync(id, jour, ct),
            Semaine = Semaine(jour, bilans, joursDouleursNotees, joursDeRegles),
        });
    }

    private async Task<IReadOnlyList<TraitementAujourdhuiViewModel>> TraitementsDuJourAsync(int carnetId, DateOnly jour, CancellationToken ct)
    {
        var debut = jour.ToDateTime(TimeOnly.MinValue);
        var fin = jour.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var traitements = await context.Medicaments.AsNoTracking()
            .Where(m => m.CarnetSanteId == carnetId && m.TraitementEnCours && m.Type == TypeTraitement.Medicamenteux)
            .OrderBy(m => m.Nom)
            .Select(m => new { m.Id, m.Nom, m.Posologie })
            .ToListAsync(ct);
        var prises = await context.DonneesMedicaments.AsNoTracking()
            .Where(p => p.CarnetSanteId == carnetId && p.Date >= debut && p.Date < fin)
            .Select(p => new { p.MedicamentId, p.Date })
            .ToListAsync(ct);

        return traitements.Select(t =>
        {
            var sesPrises = prises.Where(p => p.MedicamentId == t.Id).Select(p => p.Date).ToList();
            return new TraitementAujourdhuiViewModel
            {
                Id = t.Id,
                Nom = t.Nom,
                Posologie = t.Posologie,
                PrisesDuJour = sesPrises.Count,
                DernierePrise = sesPrises.Count == 0 ? null : sesPrises.Max(),
            };
        }).ToList();
    }

    /// <summary>
    /// 7 derniers jours (jour compris) : jours où une douleur a été notée (entrée de douleur ou bilan avec une douleur
    /// supérieure à 0), dont pendant les règles, et fatigue moyenne comparée aux 7 jours précédents.
    /// </summary>
    internal static SemaineViewModel Semaine(DateOnly jour, IReadOnlyCollection<BilanQuotidien> bilans,
        IEnumerable<DateOnly> joursDouleursNotees, IReadOnlySet<DateOnly> joursDeRegles)
    {
        var debutSemaine = jour.AddDays(-6);
        bool DansLaSemaine(DateOnly j) => j >= debutSemaine && j <= jour;

        var joursAvecDouleur = joursDouleursNotees
            .Concat(bilans.Where(b => b.DouleurMoyenne > 0).Select(b => DateOnly.FromDateTime(b.Date)))
            .Where(DansLaSemaine)
            .ToHashSet();

        double? Fatigue(Func<DateOnly, bool> periode)
        {
            var valeurs = bilans.Where(b => b.Fatigue != null && periode(DateOnly.FromDateTime(b.Date))).Select(b => (double)b.Fatigue!).ToList();
            return valeurs.Count == 0 ? null : Math.Round(valeurs.Average(), 1);
        }

        return new SemaineViewModel
        {
            JoursAvecDouleur = joursAvecDouleur.Count,
            JoursAvecDouleurPendantRegles = joursAvecDouleur.Count(joursDeRegles.Contains),
            FatigueMoyenne = Fatigue(DansLaSemaine),
            FatigueMoyennePrecedente = Fatigue(j => j >= debutSemaine.AddDays(-7) && j < debutSemaine),
        };
    }
}
