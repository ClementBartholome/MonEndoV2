using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Services.Agenda;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Controllers;

public sealed class LiaisonAgendaControllerTests : IDisposable
{
    private readonly CarnetDeTest _carnet = new();
    private readonly FauxGoogleCalendar _google = LiaisonAgendaDeTest.FauxGoogle();

    public void Dispose() => _carnet.Dispose();

    private LiaisonAgendaController Controller(bool authentifie = true, string? cookie = null, LiaisonAgendaService? service = null)
    {
        var contexte = authentifie ? CarnetDeTest.ContexteAuthentifie() : CarnetDeTest.ContexteAnonyme();
        if (cookie is not null)
        {
            contexte.HttpContext.Request.Headers.Cookie = $"{LiaisonAgendaController.CookieEtat}={cookie}";
        }

        return new LiaisonAgendaController(
            service ?? LiaisonAgendaDeTest.Service(_carnet.Context, _google), new HorlogeFixe(AgendaDeTest.Maintenant))
        {
            ControllerContext = contexte,
        };
    }

    [Fact]
    public async Task Callback_RetourApplicationConfigure_RedirigeVersCetteAdresse()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new GoogleOAuthOptions { RetourApplication = "https://localhost:5173/parametres" });
        var controller = new LiaisonAgendaController(
            LiaisonAgendaDeTest.Service(_carnet.Context, _google), new HorlogeFixe(AgendaDeTest.Maintenant), options)
        {
            ControllerContext = CarnetDeTest.ContexteAnonyme(),
        };

        var resultat = await controller.Callback(null, null, "access_denied", CancellationToken.None);

        Assert.Equal("https://localhost:5173/parametres?agenda=echec", Assert.IsType<RedirectResult>(resultat).Url);
    }

    private static int? StatutDe(IActionResult resultat) => (resultat as IStatusCodeActionResult)?.StatusCode;

    private static string EtatDe(string url) => Uri.UnescapeDataString(url.Split("state=")[1].Split('&')[0]);

    [Fact]
    public async Task Demarrer_PoseUnCookieDEtatRestreintEtRenvoieLUrlGoogle()
    {
        var controller = Controller();

        var resultat = await controller.Demarrer(CancellationToken.None);

        var url = Assert.IsType<OkObjectResult>(resultat).Value!.GetType().GetProperty("url")!.GetValue(
            Assert.IsType<OkObjectResult>(resultat).Value) as string;
        Assert.StartsWith(GoogleOAuthClient.UrlAutorisation, url);
        var cookie = controller.Response.Headers.SetCookie.ToString();
        Assert.StartsWith($"{LiaisonAgendaController.CookieEtat}=", cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/Agenda/liaison", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Demarrer_LiaisonNonConfiguree_503SansCookie()
    {
        var service = LiaisonAgendaDeTest.Service(_carnet.Context, _google, LiaisonAgendaDeTest.Options(false));
        var controller = Controller(service: service);

        var resultat = await controller.Demarrer(CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, StatutDe(resultat));
        Assert.Equal(0, controller.Response.Headers.SetCookie.Count);
    }

    [Fact]
    public async Task Callback_RetourValideSansSession_LieEtRedirigeVersParametres()
    {
        var service = LiaisonAgendaDeTest.Service(_carnet.Context, _google);
        var debut = (await service.DemarrerAsync(CarnetDeTest.UserId, CancellationToken.None)).Valeur!;
        // Retour de Google : le cookie de session n'est pas envoyé (SameSite=Strict), seul le cookie d'état l'est.
        var controller = Controller(authentifie: false, cookie: debut.CookieEtat, service: service);

        var resultat = await controller.Callback(EtatDe(debut.UrlAutorisation), LiaisonAgendaDeTest.Code, null, CancellationToken.None);

        Assert.Equal("/parametres?agenda=lie", Assert.IsType<RedirectResult>(resultat).Url);
        Assert.True((await service.StatutAsync(CarnetDeTest.UserId, CancellationToken.None)).Liee);
        Assert.Contains(LiaisonAgendaController.CookieEtat + "=;", controller.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public async Task Callback_EtatForge_EchecSansLierEtEfface()
    {
        var service = LiaisonAgendaDeTest.Service(_carnet.Context, _google);
        var debut = (await service.DemarrerAsync(CarnetDeTest.UserId, CancellationToken.None)).Valeur!;
        var controller = Controller(authentifie: false, cookie: debut.CookieEtat, service: service);

        var resultat = await controller.Callback("etat-forge", LiaisonAgendaDeTest.Code, null, CancellationToken.None);

        Assert.Equal("/parametres?agenda=echec", Assert.IsType<RedirectResult>(resultat).Url);
        Assert.False((await service.StatutAsync(CarnetDeTest.UserId, CancellationToken.None)).Liee);
        Assert.Empty(_google.Requetes);
    }

    [Fact]
    public async Task Callback_SansCookie_Echec()
    {
        var resultat = await Controller(authentifie: false).Callback("x", LiaisonAgendaDeTest.Code, null, CancellationToken.None);

        Assert.Equal("/parametres?agenda=echec", Assert.IsType<RedirectResult>(resultat).Url);
    }

    [Fact]
    public async Task Callback_RefusDeLUtilisatrice_Echec()
    {
        var service = LiaisonAgendaDeTest.Service(_carnet.Context, _google);
        var debut = (await service.DemarrerAsync(CarnetDeTest.UserId, CancellationToken.None)).Valeur!;
        var controller = Controller(authentifie: false, cookie: debut.CookieEtat, service: service);

        var resultat = await controller.Callback(EtatDe(debut.UrlAutorisation), null, "access_denied", CancellationToken.None);

        Assert.Equal("/parametres?agenda=echec", Assert.IsType<RedirectResult>(resultat).Url);
    }

    [Fact]
    public async Task GetStatut_RenvoieLeStatutDeLUtilisatrice()
    {
        var resultat = await Controller().GetStatut(CancellationToken.None);

        var statut = Assert.IsType<StatutLiaison>(Assert.IsType<OkObjectResult>(resultat).Value);
        Assert.True(statut.Disponible);
        Assert.False(statut.Liee);
    }

    [Fact]
    public async Task Delier_Liee_204PuisIntrouvable()
    {
        var service = LiaisonAgendaDeTest.Service(_carnet.Context, _google);
        var debut = (await service.DemarrerAsync(CarnetDeTest.UserId, CancellationToken.None)).Valeur!;
        await service.FinaliserAsync(debut.CookieEtat, EtatDe(debut.UrlAutorisation), LiaisonAgendaDeTest.Code, null, CancellationToken.None);
        var controller = Controller(service: service);

        var premier = await controller.Delier(CancellationToken.None);
        var second = await controller.Delier(CancellationToken.None);

        Assert.IsType<NoContentResult>(premier);
        Assert.Equal(StatusCodes.Status404NotFound, StatutDe(second));
    }
}
