using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services.WebPush;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Controllers;

public sealed class NotificationsControllerTests : IDisposable
{
    private readonly CarnetDeTest _carnet = new();
    private readonly FauxEnvoiPush _envoi = new();

    private NotificationsController Controller(bool configure = true, bool authentifie = true)
    {
        var horloge = new HorlogeFixe(new DateTimeOffset(2026, 9, 26, 12, 0, 0, TimeSpan.Zero));
        var notifications = new NotificationsPushService(
            _carnet.Context, _envoi, horloge, NullLogger<NotificationsPushService>.Instance);
        IOptions<WebPushOptions> options = configure ? PushDeTest.OptionsConfigurees() : PushDeTest.OptionsNonConfigurees();

        return new NotificationsController(new NotificationsService(_carnet.Context, notifications, options, horloge))
        {
            ControllerContext = authentifie ? CarnetDeTest.ContexteAuthentifie() : CarnetDeTest.ContexteAnonyme(),
        };
    }

    private static AbonnementPushDto AbonnementDto(string endpoint = "https://push.example/appareil") =>
        new() { Endpoint = endpoint, P256dh = "cle-p256dh", Auth = "secret-auth" };

    private static int? StatutDe(IActionResult resultat) => (resultat as IStatusCodeActionResult)?.StatusCode;

    private static PreferenceRappelDto Preferences(IActionResult resultat) =>
        Assert.IsType<PreferenceRappelDto>(Assert.IsType<OkObjectResult>(resultat).Value);

    [Fact]
    public void GetClePublique_Configuree_RetourneLaCle()
    {
        Assert.IsType<OkObjectResult>(Controller().GetClePublique());
    }

    [Fact]
    public void GetClePublique_NonConfiguree_Retourne503()
    {
        Assert.Equal(503, StatutDe(Controller(configure: false).GetClePublique()));
    }

    [Fact]
    public async Task Abonner_EndpointHttps_EnregistreLAppareilSurLeCarnetConnecte()
    {
        var resultat = await Controller().Abonner(AbonnementDto(), CancellationToken.None);

        Assert.IsType<NoContentResult>(resultat);
        var abonnement = Assert.Single(_carnet.Context.AbonnementsPush);
        Assert.Equal(CarnetDeTest.CarnetSanteId, abonnement.CarnetSanteId);
        Assert.Equal("cle-p256dh", abonnement.P256dh);
    }

    [Theory]
    [InlineData("http://push.example/appareil")]
    [InlineData("pas-une-url")]
    public async Task Abonner_EndpointNonHttps_RetourneBadRequest(string endpoint)
    {
        var resultat = await Controller().Abonner(AbonnementDto(endpoint), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultat);
        Assert.Empty(_carnet.Context.AbonnementsPush);
    }

    [Fact]
    public async Task Abonner_AppareilDejaLieAUnAutreCompte_EstReaffecteAuCompteConnecte()
    {
        _carnet.Context.AbonnementsPush.Add(new AbonnementPush
        {
            CarnetSanteId = CarnetDeTest.AutreCarnetSanteId,
            Endpoint = "https://push.example/appareil",
            P256dh = "ancienne",
            Auth = "ancienne",
        });
        _carnet.Context.SaveChanges();

        await Controller().Abonner(AbonnementDto(), CancellationToken.None);

        var abonnement = Assert.Single(_carnet.Context.AbonnementsPush);
        Assert.Equal(CarnetDeTest.CarnetSanteId, abonnement.CarnetSanteId);
        Assert.Equal("cle-p256dh", abonnement.P256dh);
    }

    [Fact]
    public async Task Desabonner_NeSupprimeQueLesAppareilsDuCarnetConnecte()
    {
        _carnet.Context.AbonnementsPush.AddRange(
            new AbonnementPush { CarnetSanteId = CarnetDeTest.CarnetSanteId, Endpoint = "https://push.example/moi", P256dh = "k", Auth = "a" },
            new AbonnementPush { CarnetSanteId = CarnetDeTest.AutreCarnetSanteId, Endpoint = "https://push.example/autre", P256dh = "k", Auth = "a" });
        _carnet.Context.SaveChanges();
        var controller = Controller();

        await controller.Desabonner(new DesabonnementPushDto { Endpoint = "https://push.example/moi" }, CancellationToken.None);
        var resultat = await controller.Desabonner(new DesabonnementPushDto { Endpoint = "https://push.example/autre" }, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultat);
        var restant = Assert.Single(_carnet.Context.AbonnementsPush);
        Assert.Equal("https://push.example/autre", restant.Endpoint);
    }

    [Fact]
    public async Task GetPreferences_SansReglage_RetourneLesValeursParDefaut()
    {
        var preferences = Preferences(await Controller().GetPreferences(CancellationToken.None));

        Assert.False(preferences.RappelActif);
        Assert.Equal("21:00", preferences.HeureRappel);
        Assert.Equal("Europe/Paris", preferences.FuseauHoraire);
    }

    [Fact]
    public async Task PutPreferences_ValeursValides_EnregistrePuisMetAJour()
    {
        var controller = Controller();

        await controller.PutPreferences(
            new PreferenceRappelDto { RappelActif = true, HeureRappel = "20:30", FuseauHoraire = "Europe/Paris" },
            CancellationToken.None);
        var resultat = await controller.PutPreferences(
            new PreferenceRappelDto { RappelActif = true, HeureRappel = "07:45", FuseauHoraire = "America/New_York" },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(resultat);
        var preferences = Preferences(await controller.GetPreferences(CancellationToken.None));
        Assert.True(preferences.RappelActif);
        Assert.Equal("07:45", preferences.HeureRappel);
        Assert.Equal("America/New_York", preferences.FuseauHoraire);
    }

    [Theory]
    [InlineData("25:00", "Europe/Paris")]
    [InlineData("9h", "Europe/Paris")]
    [InlineData("21:00", "Fuseau/Inexistant")]
    public async Task PutPreferences_ValeursInvalides_RetourneBadRequest(string heure, string fuseau)
    {
        var resultat = await Controller().PutPreferences(
            new PreferenceRappelDto { RappelActif = true, HeureRappel = heure, FuseauHoraire = fuseau },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultat);
        Assert.Empty(_carnet.Context.PreferencesRappel);
    }

    [Fact]
    public async Task EnvoyerTest_AppareilAbonne_EnvoieLaNotification()
    {
        await Controller().Abonner(AbonnementDto(), CancellationToken.None);

        var resultat = await Controller().EnvoyerTest(CancellationToken.None);

        Assert.IsType<OkObjectResult>(resultat);
        Assert.Single(_envoi.Envois);
    }

    [Fact]
    public async Task EnvoyerTest_AucunAppareil_RetourneBadRequest()
    {
        Assert.IsType<BadRequestObjectResult>(await Controller().EnvoyerTest(CancellationToken.None));
    }

    [Fact]
    public async Task EnvoyerTest_NonConfigure_Retourne503()
    {
        Assert.Equal(503, StatutDe(await Controller(configure: false).EnvoyerTest(CancellationToken.None)));
    }

    [Fact]
    public async Task ToutesLesActions_SansUtilisatriceConnectee_RetournentUnauthorized()
    {
        var controller = Controller(authentifie: false);

        Assert.IsType<UnauthorizedResult>(await controller.Abonner(AbonnementDto(), CancellationToken.None));
        Assert.IsType<UnauthorizedResult>(await controller.Desabonner(new DesabonnementPushDto { Endpoint = "x" }, CancellationToken.None));
        Assert.IsType<UnauthorizedResult>(await controller.GetPreferences(CancellationToken.None));
        Assert.IsType<UnauthorizedResult>(await controller.PutPreferences(new PreferenceRappelDto(), CancellationToken.None));
        Assert.IsType<UnauthorizedResult>(await controller.EnvoyerTest(CancellationToken.None));
    }

    public void Dispose() => _carnet.Dispose();
}
