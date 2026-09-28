using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Accueil;
using MonEndoVue.Server.Services.Traitements;
using MonEndoVue.Server.Tests.Support;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>Accueil « Aujourd'hui » : données du jour, de la semaine, cloisonnement par carnet.</summary>
public sealed class AccueilServiceTests : IDisposable
{
    private static readonly DateOnly Jour = new(2026, 9, 28);
    private static readonly DateTimeOffset Maintenant = new(2026, 9, 28, 9, 0, 0, TimeSpan.Zero);

    private readonly CarnetDeTest _carnet = new();

    private static DateTime A(int decalageJours, int heure = 9) => Jour.AddDays(decalageJours).ToDateTime(new TimeOnly(heure, 0));

    [Fact]
    public async Task GetAujourdhui_RegroupeCycleBilanTraitementsEtSemaine()
    {
        var ctx = _carnet.Context;
        const int c = CarnetDeTest.CarnetSanteId;
        ctx.JourRegles.AddRange(new JourRegle { CarnetSanteId = c, Date = A(-1) }, new JourRegle { CarnetSanteId = c, Date = A(0) });
        ctx.BilansQuotidiens.AddRange(
            new BilanQuotidien { CarnetSanteId = c, Date = A(0), DouleurMoyenne = 4, Fatigue = 2, Emotions = [new EmotionBilan { Emotion = Emotion.Calme }] },
            new BilanQuotidien { CarnetSanteId = c, Date = A(-8), DouleurMoyenne = 0, Fatigue = 4 });
        ctx.DonneesDouleurs.AddRange(
            new DonneesDouleur { CarnetSanteId = c, TypeDouleur = "Douleur pelvienne", Intensite = 6, Date = A(-1) },
            new DonneesDouleur { CarnetSanteId = c, TypeDouleur = "Douleur lombaire", Intensite = 3, Date = A(-3) });
        var enCours = new Medicament
        {
            CarnetSanteId = c, Nom = "Diénogest", Type = TypeTraitement.Medicamenteux, TraitementEnCours = true, Posologie = "1 comprimé",
            DateDebutTraitement = A(-30), Frequence = FrequencePrise.ChaqueJour, Horaires = [new HorairePrise { Heure = new TimeOnly(8, 0) }],
        };
        ctx.Medicaments.AddRange(enCours,
            new Medicament { CarnetSanteId = c, Nom = "Ibuprofène", Type = TypeTraitement.Medicamenteux, TraitementEnCours = true, DateDebutTraitement = A(-30) },
            new Medicament { CarnetSanteId = c, Nom = "Ancien", Type = TypeTraitement.Medicamenteux, TraitementEnCours = false },
            new Medicament { CarnetSanteId = c, Nom = "Yoga", Type = TypeTraitement.NonMedicamenteux, TraitementEnCours = true });
        await ctx.SaveChangesAsync();
        ctx.DonneesMedicaments.AddRange(
            new DonneesMedicament { CarnetSanteId = c, MedicamentId = enCours.Id, NombreComprimes = 1, Date = A(0, 8), HeurePrevue = new TimeOnly(8, 0) },
            new DonneesMedicament { CarnetSanteId = c, MedicamentId = enCours.Id, NombreComprimes = 1, Date = A(-1, 8) });
        await ctx.SaveChangesAsync();

        var aujourdhui = await Obtenir();

        Assert.True(aujourdhui.Cycle.EnRegles);
        Assert.Equal(2, aujourdhui.Cycle.JourDeRegles);
        Assert.Equal(4, aujourdhui.Bilan!.DouleurMoyenne);
        Assert.Equal(["Calme"], aujourdhui.Bilan.Emotions);
        var prise = Assert.Single(aujourdhui.PrisesPrevues);
        Assert.Equal("Diénogest", prise.Nom);
        Assert.Equal("08:00", prise.HeurePrevue);
        Assert.Equal("Pris", prise.Reponse!.Statut);
        Assert.Equal("Ibuprofène", Assert.Single(aujourdhui.AuBesoin).Nom);
        // Douleur notée : J-3 (lombaire), J-1 (pelvienne, en règles), J0 (bilan à 4, en règles).
        Assert.Equal(3, aujourdhui.Semaine.JoursAvecDouleur);
        Assert.Equal(2, aujourdhui.Semaine.JoursAvecDouleurPendantRegles);
        Assert.Equal(2, aujourdhui.Semaine.FatigueMoyenne);
        Assert.Equal(4, aujourdhui.Semaine.FatigueMoyennePrecedente);
    }

    [Fact]
    public async Task GetAujourdhui_SansDonnees_RienAAfficher()
    {
        var aujourdhui = await Obtenir();

        Assert.Null(aujourdhui.Bilan);
        Assert.Empty(aujourdhui.PrisesPrevues);
        Assert.Empty(aujourdhui.AuBesoin);
        Assert.False(aujourdhui.Cycle.EnRegles);
        Assert.Null(aujourdhui.Cycle.JourDuCycle);
        Assert.Equal(0, aujourdhui.Semaine.JoursAvecDouleur);
        Assert.Null(aujourdhui.Semaine.FatigueMoyenne);
    }

    [Fact]
    public async Task GetAujourdhui_IgnoreLesDonneesDUnAutreCarnet()
    {
        const int autre = CarnetDeTest.AutreCarnetSanteId;
        _carnet.Context.BilansQuotidiens.Add(new BilanQuotidien { CarnetSanteId = autre, Date = A(0), DouleurMoyenne = 8 });
        _carnet.Context.JourRegles.Add(new JourRegle { CarnetSanteId = autre, Date = A(0) });
        _carnet.Context.Medicaments.Add(new Medicament { CarnetSanteId = autre, Nom = "Autre", Type = TypeTraitement.Medicamenteux, TraitementEnCours = true });
        await _carnet.Context.SaveChangesAsync();

        var aujourdhui = await Obtenir();

        Assert.Null(aujourdhui.Bilan);
        Assert.False(aujourdhui.Cycle.EnRegles);
        Assert.Empty(aujourdhui.PrisesPrevues);
        Assert.Empty(aujourdhui.AuBesoin);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(-3)]
    public async Task GetAujourdhui_JourTropEloigne_Invalide(int decalage)
    {
        var resultat = await Service().GetAujourdhuiAsync(CarnetDeTest.UserId, Jour.AddDays(decalage), CancellationToken.None);

        Assert.Equal(StatutOperation.Invalide, resultat.Statut);
    }

    [Fact]
    public async Task GetAujourdhui_UtilisatriceSansCarnet_NonAuthentifiee()
    {
        var resultat = await Service().GetAujourdhuiAsync("inconnue", Jour, CancellationToken.None);

        Assert.Equal(StatutOperation.NonAuthentifie, resultat.Statut);
    }

    [Fact]
    public async Task Controleur_RenvoieLAccueil()
    {
        var controller = new AccueilController(Service()) { ControllerContext = CarnetDeTest.ContexteAuthentifie() };

        var resultat = await controller.GetAujourdhui(Jour, CancellationToken.None);

        Assert.IsType<AujourdhuiViewModel>(Assert.IsType<OkObjectResult>(resultat).Value);
    }

    private AccueilService Service() => new(_carnet.Context, new HorlogeFixe(Maintenant),
        new TraitementsService(_carnet.Context, _carnet.CarnetSanteService, new HorlogeFixe(Maintenant)));

    private async Task<AujourdhuiViewModel> Obtenir()
    {
        var resultat = await Service().GetAujourdhuiAsync(CarnetDeTest.UserId, Jour, CancellationToken.None);
        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        return resultat.Valeur!;
    }

    public void Dispose() => _carnet.Dispose();
}
