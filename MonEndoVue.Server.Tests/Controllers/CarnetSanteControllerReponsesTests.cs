using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Controllers;

/// <summary>Réponses du carnet : données de l'utilisatrice connectée, et 404 sans détail technique quand le carnet manque.</summary>
public sealed class CarnetSanteControllerReponsesTests : IDisposable
{
    private const string SansCarnetId = "utilisatrice-sans-carnet";
    private const string SansCarnetNom = "sans-carnet@local";

    private readonly CarnetDeTest _carnet = new();

    public CarnetSanteControllerReponsesTests()
    {
        _carnet.Context.Users.Add(new ApplicationUser { Id = SansCarnetId, UserName = SansCarnetNom });
        _carnet.Context.SaveChanges();
    }

    private CarnetSanteController Controller(string userId = CarnetDeTest.UserId) =>
        new(_carnet.Context, _carnet.CarnetSanteService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "Test")),
                },
            },
        };

    [Fact]
    public async Task GetCarnetSanteByUserId_SonCarnet_RetourneOk()
    {
        var resultat = await Controller().GetCarnetSanteByUserId(CarnetDeTest.UserId);

        Assert.IsType<OkObjectResult>(resultat.Result);
    }

    [Fact]
    public async Task GetCarnetSanteByUserId_SansCarnet_RetourneNotFoundSansMessage()
    {
        var resultat = await Controller(SansCarnetId).GetCarnetSanteByUserId(SansCarnetId);

        Assert.IsType<NotFoundResult>(resultat.Result);
    }

    [Fact]
    public async Task GetCarnetSanteByUsername_SansCarnet_RetourneNotFoundSansMessage()
    {
        var resultat = await Controller(SansCarnetId).GetCarnetSanteByUsername(SansCarnetNom);

        Assert.IsType<NotFoundResult>(resultat.Result);
    }

    [Fact]
    public async Task GetCarnetSanteById_SonCarnet_RetourneOk()
    {
        var resultat = await Controller().GetCarnetSanteById(CarnetDeTest.CarnetSanteId);

        Assert.IsType<OkObjectResult>(resultat.Result);
    }

    [Fact]
    public async Task GetDonneesCarnetSanteByMonth_SonCarnet_RetourneOk()
    {
        var resultat = await Controller().GetDonneesCarnetSanteByMonth(CarnetDeTest.CarnetSanteId, 9, 2026);

        Assert.IsType<OkObjectResult>(resultat.Result);
    }

    [Fact]
    public async Task GetCurrentUserDonneesCarnetSanteByMonth_SonCarnet_RetourneOk()
    {
        var resultat = await Controller().GetCurrentUserDonneesCarnetSanteByMonth(9, 2026);

        Assert.IsType<OkObjectResult>(resultat.Result);
    }

    [Fact]
    public async Task GetCurrentUserDonneesCarnetSanteByMonth_SansCarnet_RetourneNotFoundSansMessage()
    {
        var resultat = await Controller(SansCarnetId).GetCurrentUserDonneesCarnetSanteByMonth(9, 2026);

        Assert.IsType<NotFoundResult>(resultat.Result);
    }

    [Fact]
    public async Task GetLastEntries_SonCarnet_RetourneOk()
    {
        var resultat = await Controller().GetLastEntries(CarnetDeTest.CarnetSanteId);

        Assert.IsType<OkObjectResult>(resultat.Result);
    }

    [Fact]
    public async Task GetCurrentUserLastEntries_SonCarnet_RetourneOk()
    {
        var resultat = await Controller().GetCurrentUserLastEntries();

        Assert.IsType<OkObjectResult>(resultat.Result);
    }

    [Fact]
    public async Task GetCurrentUserLastEntries_SansCarnet_RetourneNotFoundSansMessage()
    {
        var resultat = await Controller(SansCarnetId).GetCurrentUserLastEntries();

        Assert.IsType<NotFoundResult>(resultat.Result);
    }

    [Fact]
    public async Task CreateCarnetForCurrentUser_SansCarnet_CreeSonCarnet()
    {
        var resultat = await Controller(SansCarnetId).CreateCarnetForCurrentUser();

        Assert.IsType<OkObjectResult>(resultat);
        Assert.Contains(_carnet.Context.CarnetSantes, c => c.UserId == SansCarnetId);
    }

    [Fact]
    public async Task CreateCarnetForCurrentUser_ErreurDeLaBase_Retourne500SansDetailTechnique()
    {
        var controller = Controller(SansCarnetId);
        await _carnet.Context.DisposeAsync();

        var resultat = await controller.CreateCarnetForCurrentUser();

        var erreur = Assert.IsType<ObjectResult>(resultat);
        Assert.Equal(StatusCodes.Status500InternalServerError, erreur.StatusCode);
        Assert.Equal("Erreur lors de la création du carnet", erreur.Value);
    }

    public void Dispose() => _carnet.Dispose();
}
