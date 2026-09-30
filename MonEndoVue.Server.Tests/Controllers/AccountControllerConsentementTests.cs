using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Services.Consentement;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Controllers;

/// <summary>Consentement explicite aux données de santé : à l'inscription, puis pour les comptes existants.</summary>
public sealed class AccountControllerConsentementTests : IDisposable
{
    private readonly IdentityDeTest _identity = new();

    [Fact]
    public async Task Register_SansConsentement_RefuseEtNeCreePasLeCompte()
    {
        var controller = _identity.CreerController();

        var resultat = await controller.Register(new InscriptionDto
        {
            Email = "sans-accord@local",
            Password = IdentityDeTest.MotDePasseValide,
            ConsentementDonneesSante = false,
        });

        Assert.IsType<BadRequestObjectResult>(resultat);
        Assert.Null(await _identity.UserManager.FindByEmailAsync("sans-accord@local"));
    }

    [Fact]
    public async Task Register_AvecConsentement_EnregistreLaDateEtLaVersion()
    {
        var controller = _identity.CreerController();

        var resultat = await controller.Register(new InscriptionDto
        {
            Email = "accord@local",
            Password = IdentityDeTest.MotDePasseValide,
            ConsentementDonneesSante = true,
        });

        Assert.True(ConsentementAJour(resultat));
        var user = await _identity.UserManager.FindByEmailAsync("accord@local");
        Assert.Equal(IdentityDeTest.Maintenant.UtcDateTime, user!.ConsentementDonneesSanteLe);
        Assert.Equal(PolitiqueConfidentialite.Version, user.VersionPolitiqueAcceptee);
    }

    [Fact]
    public async Task Login_CompteSansConsentement_SignaleLeConsentementManquant()
    {
        await _identity.CreerUtilisatrice("ancienne@local");
        var controller = _identity.CreerController();

        var resultat = await controller.Login(new IdentifiantsDto { Email = "ancienne@local", Password = IdentityDeTest.MotDePasseValide });

        Assert.False(ConsentementAJour(resultat));
    }

    [Fact]
    public async Task DonnerConsentement_EnregistreLeConsentementEtRenouvelleLeJeton()
    {
        var user = await _identity.CreerUtilisatrice("ancienne@local");
        var controller = _identity.CreerController(user.Id);

        var resultat = await controller.DonnerConsentement();

        Assert.True(ConsentementAJour(resultat));
        Assert.True(PolitiqueConfidentialite.EstAJour((await _identity.UserManager.FindByIdAsync(user.Id))!));
        Assert.Contains(controller.Response.Headers.SetCookie, c => c!.StartsWith("accessToken="));
    }

    [Fact]
    public async Task DonnerConsentement_UtilisatriceInconnue_RetourneUnauthorized()
    {
        var controller = _identity.CreerController("inconnue");

        Assert.IsType<UnauthorizedResult>(await controller.DonnerConsentement());
    }

    [Fact]
    public void AccountController_AccessibleSansConsentement()
    {
        // Sinon une utilisatrice sans consentement ne pourrait ni se connecter ni donner son accord.
        Assert.NotNull(typeof(AccountController).GetCustomAttribute<SansConsentementAttribute>());
    }

    private static bool ConsentementAJour(IActionResult resultat)
    {
        var ok = Assert.IsType<OkObjectResult>(resultat);
        return (bool)ok.Value!.GetType().GetProperty("ConsentementAJour")!.GetValue(ok.Value)!;
    }

    public void Dispose() => _identity.Dispose();
}
