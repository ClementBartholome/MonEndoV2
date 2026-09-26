using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Controllers;

/// <summary>Connexion, inscription et changement de mot de passe avec identifiants transmis dans le corps de la requête.</summary>
public sealed class AccountControllerIdentifiantsTests : IDisposable
{
    private readonly IdentityDeTest _identity = new();

    [Fact]
    public async Task Register_IdentifiantsValides_CreeLUtilisatriceEtSonCarnet()
    {
        var controller = _identity.CreerController();

        var resultat = await controller.Register(new IdentifiantsDto
        {
            Email = "nouvelle@local",
            Password = IdentityDeTest.MotDePasseValide,
        });

        Assert.IsType<OkObjectResult>(resultat);
        var user = await _identity.UserManager.FindByEmailAsync("nouvelle@local");
        Assert.NotNull(user);
        Assert.True(await _identity.Context.CarnetSantes.AnyAsync(c => c.UserId == user.Id));
    }

    [Fact]
    public async Task Register_MotDePasseTropFaible_RetourneBadRequest()
    {
        var controller = _identity.CreerController();

        var resultat = await controller.Register(new IdentifiantsDto { Email = "faible@local", Password = "abc" });

        Assert.IsType<BadRequestObjectResult>(resultat);
        Assert.Null(await _identity.UserManager.FindByEmailAsync("faible@local"));
    }

    [Fact]
    public async Task Login_IdentifiantsValides_RetourneOk()
    {
        await _identity.CreerUtilisatrice("connexion@local");
        var controller = _identity.CreerController();

        var resultat = await controller.Login(new IdentifiantsDto
        {
            Email = "connexion@local",
            Password = IdentityDeTest.MotDePasseValide,
        });

        Assert.IsType<OkObjectResult>(resultat);
    }

    [Theory]
    [InlineData("connexion@local", "MauvaisMotDePasse1!")]
    [InlineData("inconnue@local", IdentityDeTest.MotDePasseValide)]
    public async Task Login_IdentifiantsInvalides_RetourneUnauthorized(string email, string motDePasse)
    {
        await _identity.CreerUtilisatrice("connexion@local");
        var controller = _identity.CreerController();

        var resultat = await controller.Login(new IdentifiantsDto { Email = email, Password = motDePasse });

        Assert.IsType<UnauthorizedResult>(resultat);
    }

    [Fact]
    public async Task Login_ErreurInterne_RetourneUn500SansDetailTechnique()
    {
        await _identity.CreerUtilisatrice("sans-carnet@local", avecCarnet: false);
        var controller = _identity.CreerController();

        var resultat = await controller.Login(new IdentifiantsDto
        {
            Email = "sans-carnet@local",
            Password = IdentityDeTest.MotDePasseValide,
        });

        var erreur = Assert.IsType<ObjectResult>(resultat);
        Assert.Equal(500, erreur.StatusCode);
        Assert.DoesNotContain("introuvable", erreur.Value?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task ChangePassword_MotDePasseActuelCorrect_ChangeLeMotDePasse()
    {
        var user = await _identity.CreerUtilisatrice("changement@local");
        var controller = _identity.CreerController(user.Id);

        var resultat = await controller.ChangePassword(new ChangementMotDePasseDto
        {
            CurrentPassword = IdentityDeTest.MotDePasseValide,
            NewPassword = "NouveauMotDePasse2!",
        });

        Assert.IsType<OkResult>(resultat);
        Assert.True(await _identity.UserManager.CheckPasswordAsync(user, "NouveauMotDePasse2!"));
    }

    [Fact]
    public async Task ChangePassword_MotDePasseActuelIncorrect_RetourneBadRequest()
    {
        var user = await _identity.CreerUtilisatrice("changement@local");
        var controller = _identity.CreerController(user.Id);

        var resultat = await controller.ChangePassword(new ChangementMotDePasseDto
        {
            CurrentPassword = "Incorrect1!",
            NewPassword = "NouveauMotDePasse2!",
        });

        Assert.IsType<BadRequestObjectResult>(resultat);
    }

    public void Dispose() => _identity.Dispose();
}
