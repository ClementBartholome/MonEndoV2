using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Controllers;

public class BilanQuotidienControllerTransitTests : IDisposable
{
    private readonly CarnetDeTest _carnet = new();
    private readonly BilanQuotidienController _controller;

    public BilanQuotidienControllerTransitTests()
    {
        _controller = new BilanQuotidienController(_carnet.Context, _carnet.CarnetSanteService, TimeProvider.System)
        {
            ControllerContext = CarnetDeTest.ContexteAuthentifie(),
        };
    }

    private static BilanQuotidien Bilan() => new()
    {
        CarnetSanteId = CarnetDeTest.CarnetSanteId,
        Date = new DateTime(2026, 9, 26),
        Mood = "Neutre",
    };

    [Fact]
    public async Task Post_TransitCoherent_EnregistreLeBilanAvecLeTransit()
    {
        var bilan = Bilan();
        bilan.Selles = true;
        bilan.TypeBristol = 4;
        bilan.Ballonnements = true;
        bilan.IntensiteBallonnements = "Légère";

        var resultat = await _controller.PostBilanQuotidien(bilan);

        Assert.IsType<CreatedAtActionResult>(resultat.Result);
        var enregistre = Assert.Single(_carnet.Context.BilansQuotidiens);
        Assert.Equal(4, enregistre.TypeBristol);
        Assert.Equal("Légère", enregistre.IntensiteBallonnements);
    }

    [Fact]
    public async Task Post_TransitIncoherent_RetourneBadRequestSansEnregistrer()
    {
        var bilan = Bilan();
        bilan.Selles = false;
        bilan.TypeBristol = 2;

        var resultat = await _controller.PostBilanQuotidien(bilan);

        Assert.IsType<BadRequestObjectResult>(resultat.Result);
        Assert.Empty(_carnet.Context.BilansQuotidiens);
    }

    [Fact]
    public async Task Put_TransitIncoherent_RetourneBadRequest()
    {
        var bilan = Bilan();
        bilan.Id = 1;
        bilan.CrampesEstomac = true; // intensité manquante

        var resultat = await _controller.PutBilanQuotidien(1, bilan);

        Assert.IsType<BadRequestObjectResult>(resultat);
    }

    [Fact]
    public async Task Post_CategoriesFacultatives_SontEnregistreesTellesQuelles()
    {
        var bilan = Bilan();
        bilan.DouleurUriner = true;
        bilan.IntensiteDouleurUriner = "Forte";
        bilan.SangUrines = false;
        bilan.Nuit = "Difficile";
        bilan.LimitationJournee = "TresLimitee";
        bilan.DouleurRapport = "PasDeRapport";

        var resultat = await _controller.PostBilanQuotidien(bilan);

        Assert.IsType<CreatedAtActionResult>(resultat.Result);
        var enregistre = Assert.Single(_carnet.Context.BilansQuotidiens);
        Assert.Equal(("Forte", false, "Difficile", "TresLimitee", "PasDeRapport"),
            (enregistre.IntensiteDouleurUriner, enregistre.SangUrines, enregistre.Nuit, enregistre.LimitationJournee, enregistre.DouleurRapport));
        // Ce qui n'a pas été répondu reste « non renseigné » : jamais « non » ni zéro par défaut.
        Assert.Null(enregistre.Nausees);
        Assert.Null(enregistre.SaignementsHorsRegles);
        Assert.Null(enregistre.AbsenceTravail);
    }

    [Fact]
    public async Task Post_CategorieIncoherente_RetourneBadRequestSansEnregistrer()
    {
        var bilan = Bilan();
        bilan.DouleurUriner = false;
        bilan.IntensiteDouleurUriner = "Forte";

        var resultat = await _controller.PostBilanQuotidien(bilan);

        Assert.IsType<BadRequestObjectResult>(resultat.Result);
        Assert.Empty(_carnet.Context.BilansQuotidiens);
    }

    [Fact]
    public async Task Put_CategoriesFacultatives_RemplacentLesReponsesPrecedentes()
    {
        var bilan = Bilan();
        bilan.Nuit = "Bonne";
        bilan.ReveilsDouleur = true;
        bilan.SaignementsHorsRegles = true;
        bilan.AbondanceSaignementsHorsRegles = "Traces";
        _carnet.Context.BilansQuotidiens.Add(bilan);
        await _carnet.Context.SaveChangesAsync();
        var modifie = Bilan();
        modifie.Id = bilan.Id;
        modifie.Nuit = "Difficile";
        // Les saignements et les réveils ne sont plus renseignés : ils redeviennent « non renseignés ».

        var resultat = await _controller.PutBilanQuotidien(bilan.Id, modifie);

        Assert.IsType<NoContentResult>(resultat);
        var apres = Assert.Single(_carnet.Context.BilansQuotidiens);
        Assert.Equal("Difficile", apres.Nuit);
        Assert.Null(apres.ReveilsDouleur);
        Assert.Null(apres.SaignementsHorsRegles);
        Assert.Null(apres.AbondanceSaignementsHorsRegles);
    }

    public void Dispose() => _carnet.Dispose();
}
