using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services.Activite;
using MonEndoVue.Server.Services.Cycle;
using MonEndoVue.Server.Services.Traitements;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Services.Export;

/// <summary>
/// Synthèse du suivi de l'utilisatrice connectée sur une période, pour le PDF « Préparer un rendez-vous » : comptes et
/// moyennes de ce qui a été noté, et détail jour par jour. Lecture seule, carnet déduit de la session, aucune interprétation.
/// </summary>
public class SyntheseRendezVousService(AppDbContext context)
{
    /// <summary>Période la plus longue acceptée (un an) : la réponse et le PDF restent d'une taille raisonnable.</summary>
    public const int JoursMaximum = 366;

    private const int EmotionsListees = 5;

    public async Task<ResultatOperation<SyntheseRendezVousViewModel>> GetAsync(string userId, DateOnly du, DateOnly au, CancellationToken ct)
    {
        if (await CarnetDeAsync(userId, ct) is not { } carnetId)
            return ResultatOperation<SyntheseRendezVousViewModel>.Echec(StatutOperation.NonAuthentifie);
        if (au < du) return ResultatOperation<SyntheseRendezVousViewModel>.Echec(StatutOperation.Invalide, "La fin de la période précède son début.");
        if (au.DayNumber - du.DayNumber >= JoursMaximum)
            return ResultatOperation<SyntheseRendezVousViewModel>.Echec(StatutOperation.Invalide, "La période ne peut pas dépasser un an.");

        var debut = du.ToDateTime(TimeOnly.MinValue);
        var fin = au.AddDays(1).ToDateTime(TimeOnly.MinValue);

        // Tous les jours de règles (quelques-uns par mois) : les cycles de la période dépendent des règles voisines.
        var lignesRegles = await context.JourRegles.AsNoTracking()
            .Where(j => j.CarnetSanteId == carnetId)
            .Select(j => new { j.Date, j.Flux, j.Caillots })
            .ToListAsync(ct);
        var toutesLesRegles = lignesRegles.Select(j => DateOnly.FromDateTime(j.Date)).Distinct().ToList();
        var joursDeRegles = toutesLesRegles.Where(j => j >= du && j <= au).ToHashSet();

        var douleurs = await context.DonneesDouleurs.AsNoTracking()
            .Where(d => d.CarnetSanteId == carnetId && d.Date >= debut && d.Date < fin)
            .ToListAsync(ct);
        var symptomes = await context.SymptomesCycles.AsNoTracking()
            .Where(s => s.CarnetSanteId == carnetId && s.Date >= debut && s.Date < fin)
            .ToListAsync(ct);
        var bilans = await context.BilansQuotidiens.AsNoTracking()
            .Where(b => b.CarnetSanteId == carnetId && b.Date >= debut && b.Date < fin)
            .OrderBy(b => b.Date)
            .ToListAsync(ct);
        var activites = await context.DonneesActivitePhysique.AsNoTracking()
            .Where(a => a.CarnetSanteId == carnetId && a.Date >= debut && a.Date < fin)
            .ToListAsync(ct);
        var transit = await context.DonneesTransit.AsNoTracking()
            .Where(t => t.CarnetSanteId == carnetId && t.Date >= debut && t.Date < fin)
            .OrderBy(t => t.Date)
            .ToListAsync(ct);

        return ResultatOperation<SyntheseRendezVousViewModel>.Succes(new SyntheseRendezVousViewModel
        {
            Du = Texte(du),
            Au = Texte(au),
            Regles = Regles(toutesLesRegles, joursDeRegles, du, au,
                lignesRegles.Where(j => joursDeRegles.Contains(DateOnly.FromDateTime(j.Date))).Select(j => (DateOnly.FromDateTime(j.Date), j.Flux, j.Caillots))),
            Douleurs = Douleurs(douleurs, joursDeRegles),
            Symptomes = Symptomes(symptomes, joursDeRegles),
            Traitements = await TraitementsAsync(carnetId, du, au, ct),
            Bilans = Bilans(bilans),
            Activite = Activite(activites),
            Transit = transit.Select(t => new SyntheseEvenementViewModel { Jour = Texte(t.Date), Type = t.TypeEvenement }).ToList(),
        });
    }

    private static SyntheseReglesViewModel Regles(IReadOnlyList<DateOnly> toutes, HashSet<DateOnly> dansLaPeriode, DateOnly du, DateOnly au,
        IEnumerable<(DateOnly Jour, FluxRegles? Flux, bool? Caillots)> details)
    {
        // Un jour par jour de règles, même si d'anciennes saisies en ont dupliqué la ligne.
        var parJour = details.GroupBy(d => d.Jour).Select(g => (
            Flux: g.Select(d => d.Flux).FirstOrDefault(f => f is not null),
            Caillots: g.Select(d => d.Caillots).FirstOrDefault(c => c is not null))).ToList();
        var regles = HistoriqueCycles.Regrouper(toutes);
        var cycles = HistoriqueCycles.Cycles(regles, int.MaxValue).Where(c => c.Debut >= du && c.Debut <= au).ToList();
        return new SyntheseReglesViewModel
        {
            Jours = dansLaPeriode.Order().Select(Texte).ToList(),
            Debuts = regles.Where(r => r.Debut >= du && r.Debut <= au).Select(r => Texte(r.Debut)).ToList(),
            CycleMoyen = HistoriqueCycles.DureeMoyenne(cycles),
            ReglesMoyenne = HistoriqueCycles.ReglesMoyenne(cycles),
            Flux = new SyntheseFluxViewModel
            {
                Traces = parJour.Count(j => j.Flux == FluxRegles.Traces),
                Leger = parJour.Count(j => j.Flux == FluxRegles.Leger),
                Moyen = parJour.Count(j => j.Flux == FluxRegles.Moyen),
                Abondant = parJour.Count(j => j.Flux == FluxRegles.Abondant),
                NonPrecise = dansLaPeriode.Count - parJour.Count(j => j.Flux is not null),
                JoursAvecCaillots = parJour.Count(j => j.Caillots == true),
                JoursSansCaillots = parJour.Count(j => j.Caillots == false),
            },
        };
    }

    private static SyntheseDouleursViewModel Douleurs(List<DonneesDouleur> douleurs, HashSet<DateOnly> joursDeRegles)
    {
        var joursForts = douleurs.Where(d => d.Intensite >= CycleService.SeuilDouleurForte).Select(d => Jour(d.Date)).ToHashSet();
        return new SyntheseDouleursViewModel
        {
            Jours = douleurs.Select(d => Jour(d.Date)).Distinct().Count(),
            JoursDouleurForte = joursForts.Count,
            JoursDouleurFortePendantRegles = joursForts.Count(joursDeRegles.Contains),
            ParType = douleurs
                .GroupBy(d => d.TypeDouleur)
                .Select(g =>
                {
                    var jours = g.Select(d => Jour(d.Date)).Distinct().ToList();
                    return new SyntheseTypeDouleurViewModel
                    {
                        Type = g.Key,
                        Jours = jours.Count,
                        IntensiteMoyenne = Arrondi(g.Average(d => d.Intensite)),
                        IntensiteMax = g.Max(d => d.Intensite),
                        JoursPendantRegles = jours.Count(joursDeRegles.Contains),
                    };
                })
                .OrderByDescending(t => t.Jours).ThenBy(t => t.Type)
                .ToList(),
            Entrees = douleurs.OrderBy(d => d.Date)
                .Select(d => new SyntheseEntreeIntensiteViewModel { Jour = Texte(d.Date), Type = d.TypeDouleur, Intensite = d.Intensite })
                .ToList(),
        };
    }

    private static SyntheseSymptomesViewModel Symptomes(List<SymptomeCycle> symptomes, HashSet<DateOnly> joursDeRegles) => new()
    {
        ParType = symptomes
            .GroupBy(s => s.TypeSymptome)
            .Select(g =>
            {
                var jours = g.Select(s => Jour(s.Date)).Distinct().ToList();
                return new SyntheseTypeSymptomeViewModel
                {
                    Type = g.Key,
                    Jours = jours.Count,
                    IntensiteMoyenne = Arrondi(g.Average(s => s.Intensite)),
                    JoursPendantRegles = jours.Count(joursDeRegles.Contains),
                };
            })
            .OrderByDescending(t => t.Jours).ThenBy(t => t.Type)
            .ToList(),
        Entrees = symptomes.OrderBy(s => s.Date)
            .Select(s => new SyntheseEntreeIntensiteViewModel { Jour = Texte(s.Date), Type = s.TypeSymptome, Intensite = s.Intensite })
            .ToList(),
    };

    /// <summary>
    /// Traitements suivis pendant la période (dates qui la recoupent) ou qui y ont une prise ou une séance notée, les
    /// traitements en cours d'abord. Une prise ignorée n'est jamais comptée comme faite.
    /// </summary>
    private async Task<IReadOnlyList<SyntheseTraitementViewModel>> TraitementsAsync(int carnetId, DateOnly du, DateOnly au, CancellationToken ct)
    {
        var debut = du.ToDateTime(TimeOnly.MinValue);
        var fin = au.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var traitements = await context.Medicaments.AsNoTracking().Include(m => m.Horaires)
            .Where(m => m.CarnetSanteId == carnetId)
            .ToListAsync(ct);
        var prises = await context.DonneesMedicaments.AsNoTracking()
            .Where(p => p.CarnetSanteId == carnetId && p.Date >= debut && p.Date < fin)
            .Select(p => new { p.MedicamentId, p.Date, p.Statut })
            .ToListAsync(ct);
        var seances = await context.DonneesTraitementNonMedicamenteux.AsNoTracking()
            .Where(s => s.CarnetSanteId == carnetId && s.Date >= debut && s.Date < fin)
            .Select(s => new { s.MedicamentId, s.Date })
            .ToListAsync(ct);

        return traitements
            .Select(t =>
            {
                var faites = t.Type == TypeTraitement.NonMedicamenteux
                    ? seances.Where(s => s.MedicamentId == t.Id).Select(s => s.Date).ToList()
                    : prises.Where(p => p.MedicamentId == t.Id && p.Statut == StatutPrise.Pris).Select(p => p.Date).ToList();
                return new SyntheseTraitementViewModel
                {
                    Traitement = TraitementsService.Vue(t),
                    // Une fin atteinte avant la fin de la période : arrêté (le drapeau stocké peut dater d'avant cette fin).
                    EnCours = t.TraitementEnCours && (t.DateFinTraitement == null || DateOnly.FromDateTime(t.DateFinTraitement.Value) >= au),
                    Prevues = PrisesPrevues(t, du, au),
                    Faites = faites.Count,
                    Ignorees = prises.Count(p => p.MedicamentId == t.Id && p.Statut == StatutPrise.Ignore),
                    Jours = faites.Select(Jour).Distinct().Order().Select(Texte).ToList(),
                };
            })
            .Where(s => s.Faites > 0 || s.Ignorees > 0 || RecoupeLaPeriode(s.Traitement, s.EnCours, du, au))
            .OrderByDescending(s => s.EnCours).ThenBy(s => s.Traitement.Nom)
            .ToList();
    }

    private static bool RecoupeLaPeriode(TraitementViewModel traitement, bool enCours, DateOnly du, DateOnly au)
    {
        if (traitement.DateDebut > au) return false;
        if (traitement.DateFin is { } fin) return fin >= du;
        // Un traitement arrêté sans date de fin connue n'est listé que s'il a une prise dans la période.
        return enCours;
    }

    private static int PrisesPrevues(Medicament traitement, DateOnly du, DateOnly au)
    {
        var total = 0;
        for (var jour = du; jour <= au; jour = jour.AddDays(1))
        {
            if (PlanningTraitement.EstPrevuDansSesDates(traitement, jour)) total += traitement.Horaires.Count;
        }
        return total;
    }

    private static SyntheseBilansViewModel Bilans(List<BilanQuotidien> bilans) => new()
    {
        Nombre = bilans.Count,
        DouleurMoyenne = Moyenne(bilans.Select(b => (double?)b.DouleurMoyenne)),
        FatigueMoyenne = Moyenne(bilans.Select(b => (double?)b.Fatigue)),
        StressMoyen = Moyenne(bilans.Select(Stress)),
        JoursBallonnements = bilans.Count(b => b.Ballonnements == true),
        JoursCrampes = bilans.Count(b => b.CrampesEstomac == true),
        Emotions = bilans
            .SelectMany(b => b.Emotions.Select(e => e.Emotion).Distinct())
            .GroupBy(e => e)
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
            .Take(EmotionsListees)
            .Select(g => new SyntheseEmotionViewModel { Emotion = g.Key.ToString(), Jours = g.Count() })
            .ToList(),
        Jours = bilans.Select(b => new SyntheseBilanDuJourViewModel
        {
            Jour = Texte(b.Date),
            Douleur = b.DouleurMoyenne,
            Fatigue = b.Fatigue,
            Stress = Stress(b),
            Selles = b.Selles,
            TypeBristol = b.TypeBristol,
            Ballonnements = b.Ballonnements,
            Crampes = b.CrampesEstomac,
            Notes = string.IsNullOrWhiteSpace(b.Commentaire) ? null : b.Commentaire.Trim(),
        }).ToList(),
    };

    private static SyntheseActiviteViewModel Activite(List<DonneesActivitePhysique> activites) => new()
    {
        Seances = activites.Count,
        Minutes = activites.Sum(a => a.Duree),
        Soulagee = activites.Count(a => a.EffetDouleur == (int)EffetActivite.Soulagee),
        Pareille = activites.Count(a => a.EffetDouleur == (int)EffetActivite.Pareille),
        PlusForte = activites.Count(a => a.EffetDouleur == (int)EffetActivite.PlusForte),
        ParType = activites
            .GroupBy(a => a.TypeActivite)
            .Select(g => new SyntheseTypeActiviteViewModel { Type = g.Key, Seances = g.Count(), Minutes = g.Sum(a => a.Duree) })
            .OrderByDescending(t => t.Seances).ThenBy(t => t.Type)
            .ToList(),
        Entrees = activites.OrderBy(a => a.Date)
            .Select(a => new SyntheseSeanceViewModel { Jour = Texte(a.Date), Type = a.TypeActivite, Niveau = ActiviteService.NiveauDe(a) })
            .ToList(),
    };

    /// <summary>Moyenne des stress renseignés (vie pro, vie perso) : un stress non saisi ne compte pas pour 0.</summary>
    private static double? Stress(BilanQuotidien bilan) => Moyenne(new double?[] { bilan.StressPro, bilan.StressPerso });

    /// <summary>Moyenne des seules valeurs renseignées, à une décimale ; null si aucune.</summary>
    private static double? Moyenne(IEnumerable<double?> valeurs)
    {
        var renseignees = valeurs.OfType<double>().ToList();
        return renseignees.Count == 0 ? null : Arrondi(renseignees.Average());
    }

    private static double Arrondi(double valeur) => Math.Round(valeur, 1, MidpointRounding.AwayFromZero);

    private async Task<int?> CarnetDeAsync(string userId, CancellationToken ct) => string.IsNullOrEmpty(userId)
        ? null
        : await context.CarnetSantes.Where(c => c.UserId == userId).Select(c => (int?)c.Id).SingleOrDefaultAsync(ct);

    private static DateOnly Jour(DateTime date) => DateOnly.FromDateTime(date);
    private static string Texte(DateTime date) => Texte(Jour(date));
    private static string Texte(DateOnly jour) => jour.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
