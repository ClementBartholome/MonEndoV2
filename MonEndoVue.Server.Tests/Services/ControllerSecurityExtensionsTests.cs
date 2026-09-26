using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

public class ControllerSecurityExtensionsTests : IDisposable
{
    private readonly CarnetDeTest _carnet = new();

    private JourRegleController Controleur(ControllerContext contexte) =>
        new(_carnet.Context, _carnet.CarnetSanteService, NullLogger<JourRegleController>.Instance)
        {
            ControllerContext = contexte,
        };

    [Fact]
    public async Task ValidateCarnetAccess_SonCarnet_AutoriseLAcces()
    {
        var controleur = Controleur(CarnetDeTest.ContexteAuthentifie());

        var resultat = await controleur.ValidateCarnetAccess(_carnet.CarnetSanteService, CarnetDeTest.CarnetSanteId);

        Assert.Null(resultat);
    }

    [Fact]
    public async Task ValidateCarnetAccess_CarnetDUneAutreUtilisatrice_RetourneForbid()
    {
        var controleur = Controleur(CarnetDeTest.ContexteAuthentifie());

        var resultat = await controleur.ValidateCarnetAccess(_carnet.CarnetSanteService, CarnetDeTest.AutreCarnetSanteId);

        Assert.IsType<ForbidResult>(resultat);
    }

    [Fact]
    public async Task ValidateCarnetAccess_SansUtilisatriceConnectee_RetourneUnauthorized()
    {
        var controleur = Controleur(CarnetDeTest.ContexteAnonyme());

        var resultat = await controleur.ValidateCarnetAccess(_carnet.CarnetSanteService, CarnetDeTest.CarnetSanteId);

        Assert.IsType<UnauthorizedResult>(resultat);
    }

    [Fact]
    public async Task ValidateCarnetAccess_UtilisatriceSansCarnet_RetourneUnauthorized()
    {
        var contexte = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "sans-carnet")], "Test")),
            },
        };
        var controleur = Controleur(contexte);

        var resultat = await controleur.ValidateCarnetAccess(_carnet.CarnetSanteService, CarnetDeTest.CarnetSanteId);

        Assert.IsType<UnauthorizedResult>(resultat);
    }

    public void Dispose() => _carnet.Dispose();
}
