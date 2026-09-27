using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Controllers;

/// <summary>Bilan en un écran : mesures facultatives et un seul bilan par jour.</summary>
public sealed class BilanQuotidienControllerSaisieTests : IDisposable
{
    // 26/09/2026 à 22h30 UTC, soit le 27 à 0h30 à Paris.
    private static readonly HorlogeFixe Maintenant = new(new DateTimeOffset(2026, 9, 26, 22, 30, 0, TimeSpan.Zero));

    private readonly CarnetDeTest _carnet = new();
    private readonly BilanQuotidienController _controller;

    public BilanQuotidienControllerSaisieTests()
    {
        _controller = new BilanQuotidienController(_carnet.Context, _carnet.CarnetSanteService, Maintenant)
        {
            ControllerContext = CarnetDeTest.ContexteAuthentifie(),
        };
    }

    private static BilanQuotidien Bilan(DateTime date, int carnetSanteId = CarnetDeTest.CarnetSanteId) => new()
    {
        CarnetSanteId = carnetSanteId,
        Date = date,
        DouleurMoyenne = 3,
        Emotions = [new EmotionBilan { Emotion = Emotion.Calme }],
    };

    private BilanQuotidien Enregistrer(BilanQuotidien bilan)
    {
        _carnet.Context.BilansQuotidiens.Add(bilan);
        _carnet.Context.SaveChanges();
        _carnet.Context.ChangeTracker.Clear();
        return bilan;
    }

    [Fact]
    public async Task Post_BilanEssentiel_LaisseLesMesuresFacultativesNonRenseignees()
    {
        var resultat = await _controller.PostBilanQuotidien(Bilan(new DateTime(2026, 9, 26, 12, 0, 0)));

        Assert.IsType<CreatedAtActionResult>(resultat.Result);
        var enregistre = Assert.Single(_carnet.Context.BilansQuotidiens);
        Assert.Equal(3, enregistre.DouleurMoyenne);
        Assert.Null(enregistre.StressPro);
        Assert.Null(enregistre.StressPerso);
        Assert.Null(enregistre.Fatigue);
        Assert.Null(enregistre.Pas);
        Assert.Null(enregistre.Hydratation);
    }

    [Fact]
    public async Task Post_MesureHorsPlage_RetourneBadRequestSansEnregistrer()
    {
        var bilan = Bilan(new DateTime(2026, 9, 26));
        bilan.Fatigue = 9;

        var resultat = await _controller.PostBilanQuotidien(bilan);

        Assert.IsType<BadRequestObjectResult>(resultat.Result);
        Assert.Empty(_carnet.Context.BilansQuotidiens);
    }

    [Fact]
    public async Task Post_JourDejaSaisi_RetourneConflictSansDoublon()
    {
        Enregistrer(Bilan(new DateTime(2026, 9, 26, 21, 30, 0)));

        var resultat = await _controller.PostBilanQuotidien(Bilan(new DateTime(2026, 9, 26, 12, 0, 0)));

        Assert.IsType<ConflictObjectResult>(resultat.Result);
        Assert.Single(_carnet.Context.BilansQuotidiens);
    }

    [Fact]
    public async Task Post_MemeJourQueLeBilanDUneAutreUtilisatrice_EstAccepte()
    {
        Enregistrer(Bilan(new DateTime(2026, 9, 26), CarnetDeTest.AutreCarnetSanteId));

        var resultat = await _controller.PostBilanQuotidien(Bilan(new DateTime(2026, 9, 26)));

        Assert.IsType<CreatedAtActionResult>(resultat.Result);
    }

    [Fact]
    public async Task Put_MemeJour_CompleteLeBilan()
    {
        var bilan = Enregistrer(Bilan(new DateTime(2026, 9, 25, 21, 0, 0)));
        var modification = Bilan(new DateTime(2026, 9, 25, 21, 0, 0));
        modification.Id = bilan.Id;
        modification.Pas = 6000;
        modification.Commentaire = "Ajouté après coup";

        var resultat = await _controller.PutBilanQuotidien(bilan.Id, modification);

        Assert.IsType<NoContentResult>(resultat);
        var enregistre = Assert.Single(_carnet.Context.BilansQuotidiens);
        Assert.Equal((6000, "Ajouté après coup"), (enregistre.Pas, enregistre.Commentaire));
    }

    [Fact]
    public async Task Put_AncienDoublonDuMemeJour_ResteModifiable()
    {
        // Deux bilans le même jour stocké (l'ancien client envoyait minuit UTC, soit 22h ou 23h la veille).
        Enregistrer(Bilan(new DateTime(2026, 9, 4, 12, 0, 0)));
        var decale = Enregistrer(Bilan(new DateTime(2026, 9, 4, 22, 0, 0)));
        var modification = Bilan(new DateTime(2026, 9, 4, 22, 0, 0));
        modification.Id = decale.Id;
        modification.Fatigue = 2;

        var resultat = await _controller.PutBilanQuotidien(decale.Id, modification);

        Assert.IsType<NoContentResult>(resultat);
        Assert.Equal(2, _carnet.Context.BilansQuotidiens.Single(b => b.Id == decale.Id).Fatigue);
    }

    [Fact]
    public async Task Put_VersUnJourDejaSaisi_RetourneConflictSansModifier()
    {
        Enregistrer(Bilan(new DateTime(2026, 9, 24)));
        var bilan = Enregistrer(Bilan(new DateTime(2026, 9, 25)));
        var modification = Bilan(new DateTime(2026, 9, 24));
        modification.Id = bilan.Id;

        var resultat = await _controller.PutBilanQuotidien(bilan.Id, modification);

        Assert.IsType<ConflictObjectResult>(resultat);
        Assert.Equal(new DateTime(2026, 9, 25), _carnet.Context.BilansQuotidiens.Single(b => b.Id == bilan.Id).Date);
    }

    [Fact]
    public async Task Post_LendemainUtcJusteApresMinuitEnFrance_EstAccepte()
    {
        var resultat = await _controller.PostBilanQuotidien(Bilan(new DateTime(2026, 9, 27, 12, 0, 0)));

        Assert.IsType<CreatedAtActionResult>(resultat.Result);
    }

    [Fact]
    public async Task Post_JourAVenir_RetourneBadRequestSansEnregistrer()
    {
        var resultat = await _controller.PostBilanQuotidien(Bilan(new DateTime(2026, 9, 28, 12, 0, 0)));

        Assert.IsType<BadRequestObjectResult>(resultat.Result);
        Assert.Empty(_carnet.Context.BilansQuotidiens);
    }

    [Fact]
    public async Task Put_VersUnJourAVenir_RetourneBadRequest()
    {
        var bilan = Enregistrer(Bilan(new DateTime(2026, 9, 25)));
        var modification = Bilan(new DateTime(2026, 10, 1));
        modification.Id = bilan.Id;

        var resultat = await _controller.PutBilanQuotidien(bilan.Id, modification);

        Assert.IsType<BadRequestObjectResult>(resultat);
    }

    public void Dispose() => _carnet.Dispose();
}
