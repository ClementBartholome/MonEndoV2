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
        _controller = new BilanQuotidienController(_carnet.Context, _carnet.CarnetSanteService)
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

    public void Dispose() => _carnet.Dispose();
}
