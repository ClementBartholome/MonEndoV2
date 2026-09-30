using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Export;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>Synthèse pour un rendez-vous : comptes et moyennes de la période, prises des traitements, cloisonnement.</summary>
public sealed class SyntheseRendezVousServiceTests : IDisposable
{
    private static readonly DateOnly Du = new(2026, 9, 1);
    private static readonly DateOnly Au = new(2026, 9, 30);

    private readonly CarnetDeTest _carnet = new();

    private SyntheseRendezVousService Service() => new(_carnet.Context);

    private async Task<MonEndoVue.Server.ViewModels.SyntheseRendezVousViewModel> Synthese(DateOnly? du = null, DateOnly? au = null) =>
        (await Service().GetAsync(CarnetDeTest.UserId, du ?? Du, au ?? Au, CancellationToken.None)).Valeur!;

    private void Regles(params DateTime[] jours) =>
        _carnet.Context.JourRegles.AddRange(jours.Select(j => new JourRegle { CarnetSanteId = CarnetDeTest.CarnetSanteId, Date = j }));

    private void Douleur(DateTime date, string type, int intensite, int carnet = CarnetDeTest.CarnetSanteId) =>
        _carnet.Context.DonneesDouleurs.Add(new DonneesDouleur { CarnetSanteId = carnet, Date = date, TypeDouleur = type, Intensite = intensite });

    [Fact]
    public async Task Douleurs_ParTypeAvecJoursDeReglesEtDouleurForte()
    {
        Regles(new DateTime(2026, 9, 8), new DateTime(2026, 9, 9));
        Douleur(new DateTime(2026, 9, 8, 9, 0, 0), "Pelvienne", 7);
        Douleur(new DateTime(2026, 9, 8, 18, 0, 0), "Pelvienne", 5);
        Douleur(new DateTime(2026, 9, 20, 9, 0, 0), "Pelvienne", 6);
        Douleur(new DateTime(2026, 9, 9, 9, 0, 0), "Lombaire", 3);
        Douleur(new DateTime(2026, 8, 31, 23, 0, 0), "Pelvienne", 9);
        Douleur(new DateTime(2026, 9, 10, 9, 0, 0), "Pelvienne", 10, CarnetDeTest.AutreCarnetSanteId);
        await _carnet.Context.SaveChangesAsync();

        var douleurs = (await Synthese()).Douleurs;

        Assert.Equal(3, douleurs.Jours);
        Assert.Equal(2, douleurs.JoursDouleurForte);
        Assert.Equal(1, douleurs.JoursDouleurFortePendantRegles);
        Assert.Equal(["Pelvienne", "Lombaire"], douleurs.ParType.Select(t => t.Type));
        var pelvienne = douleurs.ParType[0];
        Assert.Equal((2, 6.0, 7, 1), (pelvienne.Jours, pelvienne.IntensiteMoyenne, pelvienne.IntensiteMax, pelvienne.JoursPendantRegles));
        Assert.Equal(4, douleurs.Entrees.Count);
        Assert.Equal("2026-09-08", douleurs.Entrees[0].Jour);
    }

    [Fact]
    public async Task Regles_JoursDebutsEtCycleMoyenDesCyclesCommencesDansLaPeriode()
    {
        // Règles commencées les 3 juin, 1er juillet, 31 juillet et 28 août : cycles de 28, 30 et 28 jours.
        Regles(new DateTime(2026, 6, 3), new DateTime(2026, 6, 4), new DateTime(2026, 7, 1), new DateTime(2026, 7, 2),
            new DateTime(2026, 7, 31), new DateTime(2026, 8, 28));
        await _carnet.Context.SaveChangesAsync();

        var regles = (await Synthese(new DateOnly(2026, 6, 15), new DateOnly(2026, 8, 31))).Regles;

        Assert.Equal(["2026-07-01", "2026-07-02", "2026-07-31", "2026-08-28"], regles.Jours);
        Assert.Equal(["2026-07-01", "2026-07-31", "2026-08-28"], regles.Debuts);
        // Deux cycles terminés commencés dans la période (1er juillet : 30 j, 31 juillet : 28 j).
        Assert.Equal(29, regles.CycleMoyen);
    }

    [Fact]
    public async Task Traitements_PrisesPrevuesFaitesIgnoreesEtSeances()
    {
        var quotidien = new Medicament
        {
            CarnetSanteId = CarnetDeTest.CarnetSanteId, Nom = "Diénogest", Type = TypeTraitement.Medicamenteux, TraitementEnCours = true,
            Frequence = FrequencePrise.ChaqueJour, DateDebutTraitement = new DateTime(2026, 9, 21),
            Horaires = [new HorairePrise { Heure = new TimeOnly(8, 0) }],
        };
        var kine = new Medicament
        {
            CarnetSanteId = CarnetDeTest.CarnetSanteId, Nom = "Kiné", Type = TypeTraitement.NonMedicamenteux, TraitementEnCours = true,
            DateDebutTraitement = new DateTime(2026, 1, 1),
        };
        var ancien = new Medicament
        {
            CarnetSanteId = CarnetDeTest.CarnetSanteId, Nom = "Ancienne pilule", Type = TypeTraitement.Medicamenteux, TraitementEnCours = false,
            Frequence = FrequencePrise.ChaqueJour, DateDebutTraitement = new DateTime(2025, 1, 1), DateFinTraitement = new DateTime(2026, 3, 1),
        };
        var autre = new Medicament
        {
            CarnetSanteId = CarnetDeTest.AutreCarnetSanteId, Nom = "Autre carnet", Type = TypeTraitement.Medicamenteux, TraitementEnCours = true,
            Frequence = FrequencePrise.ChaqueJour, DateDebutTraitement = new DateTime(2026, 1, 1),
        };
        _carnet.Context.Medicaments.AddRange(quotidien, kine, ancien, autre);
        await _carnet.Context.SaveChangesAsync();
        _carnet.Context.DonneesMedicaments.AddRange(
            new DonneesMedicament { CarnetSanteId = CarnetDeTest.CarnetSanteId, MedicamentId = quotidien.Id, Date = new DateTime(2026, 9, 21, 8, 5, 0), Statut = StatutPrise.Pris },
            new DonneesMedicament { CarnetSanteId = CarnetDeTest.CarnetSanteId, MedicamentId = quotidien.Id, Date = new DateTime(2026, 9, 22, 8, 5, 0), Statut = StatutPrise.Pris },
            new DonneesMedicament { CarnetSanteId = CarnetDeTest.CarnetSanteId, MedicamentId = quotidien.Id, Date = new DateTime(2026, 9, 23, 9, 0, 0), Statut = StatutPrise.Ignore });
        _carnet.Context.DonneesTraitementNonMedicamenteux.Add(
            new DonneesTraitementNonMedicamenteux { CarnetSanteId = CarnetDeTest.CarnetSanteId, MedicamentId = kine.Id, Date = new DateTime(2026, 9, 10, 17, 0, 0) });
        await _carnet.Context.SaveChangesAsync();

        var traitements = (await Synthese()).Traitements;

        // L'ancien traitement, terminé avant la période et sans prise, et celui d'un autre carnet ne sont pas listés.
        Assert.Equal(["Diénogest", "Kiné"], traitements.Select(t => t.Traitement.Nom));
        var dienogest = traitements[0];
        // Du 21 au 30 septembre : 10 prises prévues.
        Assert.Equal((10, 2, 1), (dienogest.Prevues, dienogest.Faites, dienogest.Ignorees));
        Assert.Equal(["2026-09-21", "2026-09-22"], dienogest.Jours);
        Assert.Equal((0, 1), (traitements[1].Prevues, traitements[1].Faites));
    }

    [Fact]
    public async Task Bilans_MoyennesDesSeulesValeursRenseigneesEmotionsEtNotes()
    {
        _carnet.Context.BilansQuotidiens.AddRange(
            new BilanQuotidien
            {
                CarnetSanteId = CarnetDeTest.CarnetSanteId, Date = new DateTime(2026, 9, 1), DouleurMoyenne = 4, Fatigue = 3, StressPro = 2, StressPerso = 4,
                Ballonnements = true, Commentaire = "  Journée difficile  ",
                Emotions = [new EmotionBilan { Emotion = Emotion.Calme }, new EmotionBilan { Emotion = Emotion.Tristesse }],
            },
            new BilanQuotidien
            {
                CarnetSanteId = CarnetDeTest.CarnetSanteId, Date = new DateTime(2026, 9, 2), DouleurMoyenne = 7,
                Emotions = [new EmotionBilan { Emotion = Emotion.Calme }],
            });
        await _carnet.Context.SaveChangesAsync();

        var bilans = (await Synthese()).Bilans;

        Assert.Equal(2, bilans.Nombre);
        Assert.Equal((5.5, 3.0, 3.0), (bilans.DouleurMoyenne, bilans.FatigueMoyenne, bilans.StressMoyen));
        Assert.Equal((1, 0), (bilans.JoursBallonnements, bilans.JoursCrampes));
        Assert.Equal([("Calme", 2), ("Tristesse", 1)], bilans.Emotions.Select(e => (e.Emotion, e.Jours)));
        Assert.Equal("Journée difficile", bilans.Jours[0].Notes);
        Assert.Null(bilans.Jours[1].Fatigue);
        Assert.Null(bilans.Jours[1].Stress);
    }

    [Fact]
    public async Task Activite_SeancesMinutesNiveauEtEffetSurLaDouleur()
    {
        _carnet.Context.DonneesActivitePhysique.AddRange(
            new DonneesActivitePhysique { CarnetSanteId = CarnetDeTest.CarnetSanteId, Date = new DateTime(2026, 9, 3, 18, 0, 0), TypeActivite = "Yoga", Duree = 30, NiveauIntensite = 1, EffetDouleur = (int)EffetActivite.Soulagee },
            new DonneesActivitePhysique { CarnetSanteId = CarnetDeTest.CarnetSanteId, Date = new DateTime(2026, 9, 5, 18, 0, 0), TypeActivite = "Yoga", Duree = 45, Intensite = 9 },
            new DonneesActivitePhysique { CarnetSanteId = CarnetDeTest.CarnetSanteId, Date = new DateTime(2026, 9, 6, 10, 0, 0), TypeActivite = "Marche", Duree = 60, NiveauIntensite = 2, EffetDouleur = (int)EffetActivite.PlusForte });
        await _carnet.Context.SaveChangesAsync();

        var activite = (await Synthese()).Activite;

        Assert.Equal((3, 135), (activite.Seances, activite.Minutes));
        Assert.Equal((1, 0, 1), (activite.Soulagee, activite.Pareille, activite.PlusForte));
        Assert.Equal([("Yoga", 2, 75), ("Marche", 1, 60)], activite.ParType.Select(t => (t.Type, t.Seances, t.Minutes)));
        // Une séance antérieure aux trois niveaux garde sa correspondance (9/10 = soutenue).
        Assert.Equal([1, 3, 2], activite.Entrees.Select(e => e.Niveau));
    }

    [Fact]
    public async Task Periode_RefuseeSiInverseeOuDePlusDUnAn()
    {
        var inversee = await Service().GetAsync(CarnetDeTest.UserId, Au, Du, CancellationToken.None);
        var tropLongue = await Service().GetAsync(CarnetDeTest.UserId, new DateOnly(2025, 9, 1), new DateOnly(2026, 9, 30), CancellationToken.None);
        var unAn = await Service().GetAsync(CarnetDeTest.UserId, new DateOnly(2025, 10, 1), new DateOnly(2026, 9, 30), CancellationToken.None);

        Assert.Equal(StatutOperation.Invalide, inversee.Statut);
        Assert.Equal(StatutOperation.Invalide, tropLongue.Statut);
        Assert.Equal(StatutOperation.Succes, unAn.Statut);
    }

    [Fact]
    public async Task SansSession_NonAuthentifie()
    {
        var resultat = await Service().GetAsync("", Du, Au, CancellationToken.None);

        Assert.Equal(StatutOperation.NonAuthentifie, resultat.Statut);
    }

    public void Dispose() => _carnet.Dispose();
}
