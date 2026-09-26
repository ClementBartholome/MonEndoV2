using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Controllers;

public sealed class CarnetSanteControllerAccesTests : IDisposable
{
    private readonly CarnetDeTest _carnet = new();
    private readonly CarnetSanteController _controller;

    public CarnetSanteControllerAccesTests()
    {
        _controller = new CarnetSanteController(_carnet.Context, _carnet.CarnetSanteService)
        {
            ControllerContext = CarnetDeTest.ContexteAuthentifie(),
        };
    }

    [Fact]
    public async Task GetCarnetSanteByUserId_AutreUtilisatrice_RetourneForbid()
    {
        var resultat = await _controller.GetCarnetSanteByUserId(CarnetDeTest.AutreUserId);

        Assert.IsType<ForbidResult>(resultat.Result);
    }

    [Fact]
    public async Task GetCarnetSanteByUsername_AutreUtilisatrice_RetourneForbid()
    {
        var resultat = await _controller.GetCarnetSanteByUsername(CarnetDeTest.AutreUserName);

        Assert.IsType<ForbidResult>(resultat.Result);
    }

    [Fact]
    public async Task GetCarnetSanteById_CarnetDUneAutreUtilisatrice_RetourneForbid()
    {
        var resultat = await _controller.GetCarnetSanteById(CarnetDeTest.AutreCarnetSanteId);

        Assert.IsType<ForbidResult>(resultat.Result);
    }

    [Fact]
    public async Task GetDonneesCarnetSanteByMonth_CarnetDUneAutreUtilisatrice_RetourneForbid()
    {
        var resultat = await _controller.GetDonneesCarnetSanteByMonth(CarnetDeTest.AutreCarnetSanteId, 9, 2026);

        Assert.IsType<ForbidResult>(resultat.Result);
    }

    [Fact]
    public async Task GetLastEntries_CarnetDUneAutreUtilisatrice_RetourneForbid()
    {
        var resultat = await _controller.GetLastEntries(CarnetDeTest.AutreCarnetSanteId);

        Assert.IsType<ForbidResult>(resultat.Result);
    }

    public void Dispose() => _carnet.Dispose();
}
