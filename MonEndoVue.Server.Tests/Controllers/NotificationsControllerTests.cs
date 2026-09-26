using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services.WebPush;
using MonEndoVue.Server.Services.WebPush.Rappels;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Controllers;

public sealed class NotificationsControllerTests : IDisposable
{
    private readonly CarnetDeTest _carnet = new();
    private readonly FauxEnvoiPush _envoi = new();

    private NotificationsController Controller(bool configure = true, bool authentifie = true)
    {
        var horloge = new HorlogeFixe(new DateTimeOffset(2026, 9, 26, 12, 0, 0, TimeSpan.Zero));
        IRegleRappel[] regles = [new RappelBilanQuotidien(_carnet.Context), new RappelSuiviAcne(_carnet.Context)];
        var notifications = new NotificationsPushService(
            _carnet.Context, _envoi, regles, horloge, NullLogger<NotificationsPushService>.Instance);
        IOptions<WebPushOptions> options = configure ? PushDeTest.OptionsConfigurees() : PushDeTest.OptionsNonConfigurees();

        return new NotificationsController(new NotificationsService(_carnet.Context, notifications, regles, options, horloge))
        {
            ControllerContext = authentifie ? CarnetDeTest.ContexteAuthentifie() : CarnetDeTest.ContexteAnonyme(),
        };
    }

    private static AbonnementPushDto AbonnementDto(string endpoint = "https://push.example/appareil") =>
        new() { Endpoint = endpoint, P256dh = "cle-p256dh", Auth = "secret-auth" };

    private static int? StatutDe(IActionResult resultat) => (resultat as IStatusCodeActionResult)?.StatusCode;

    private static List<RappelDto> Rappels(IActionResult resultat) =>
        Assert.IsType<List<RappelDto>>(Assert.IsType<OkObjectResult>(resultat).Value);

    private static RappelDto Reglage(bool actif, string heure, int? jour = null, string fuseau = "Europe/Paris") =>
        new() { Actif = actif, Heure = heure, JourSemaine = jour, FuseauHoraire = fuseau };

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
    public async Task GetRappels_SansReglage_RetourneTousLesTypesAvecLeursValeursParDefaut()
    {
        var rappels = Rappels(await Controller().GetRappels(CancellationToken.None));

        Assert.Collection(rappels,
            bilan =>
            {
                Assert.Equal(("BilanQuotidien", false, false, "21:00", (int?)null), (bilan.Type, bilan.EstHebdomadaire, bilan.Actif, bilan.Heure, bilan.JourSemaine));
            },
            acne =>
            {
                Assert.Equal(("SuiviAcne", true, false, "20:00", (int?)0), (acne.Type, acne.EstHebdomadaire, acne.Actif, acne.Heure, acne.JourSemaine));
            });
    }

    [Fact]
    public async Task PutRappel_ValeursValides_EnregistrePuisMetAJour()
    {
        var controller = Controller();

        await controller.PutRappel("SuiviAcne", Reglage(true, "19:30", 0), CancellationToken.None);
        var resultat = await controller.PutRappel("suiviacne", Reglage(true, "18:15", 6, "America/New_York"), CancellationToken.None);
        await controller.PutRappel("BilanQuotidien", Reglage(true, "21:30", jour: 3), CancellationToken.None);

        Assert.IsType<NoContentResult>(resultat);
        var rappels = Rappels(await controller.GetRappels(CancellationToken.None));
        var acne = rappels.Single(r => r.Type == "SuiviAcne");
        Assert.Equal((true, "18:15", (int?)6, "America/New_York"), (acne.Actif, acne.Heure, acne.JourSemaine, acne.FuseauHoraire));
        var bilan = rappels.Single(r => r.Type == "BilanQuotidien");
        Assert.Equal((true, "21:30", (int?)null), (bilan.Actif, bilan.Heure, bilan.JourSemaine)); // jour ignoré : quotidien
        Assert.Equal(2, _carnet.Context.Rappels.Count());
    }

    [Theory]
    [InlineData("BilanQuotidien", "25:00", null, "Europe/Paris")]
    [InlineData("BilanQuotidien", "9h", null, "Europe/Paris")]
    [InlineData("BilanQuotidien", "21:00", null, "Fuseau/Inexistant")]
    [InlineData("SuiviAcne", "20:00", null, "Europe/Paris")]
    [InlineData("SuiviAcne", "20:00", 7, "Europe/Paris")]
    public async Task PutRappel_ValeursInvalides_RetourneBadRequest(string type, string heure, int? jour, string fuseau)
    {
        var resultat = await Controller().PutRappel(type, Reglage(true, heure, jour, fuseau), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultat);
        Assert.Empty(_carnet.Context.Rappels);
    }

    [Fact]
    public async Task PutRappel_TypeInconnu_RetourneNotFound()
    {
        Assert.IsType<NotFoundResult>(await Controller().PutRappel("Inconnu", Reglage(true, "20:00"), CancellationToken.None));
    }

    [Fact]
    public async Task PutRappel_NeModifieQueLesRappelsDuCarnetConnecte()
    {
        _carnet.Context.Rappels.Add(new Rappel
        {
            CarnetSanteId = CarnetDeTest.AutreCarnetSanteId, Type = TypeRappel.BilanQuotidien, Actif = false, Heure = new TimeOnly(8, 0),
        });
        _carnet.Context.SaveChanges();

        await Controller().PutRappel("BilanQuotidien", Reglage(true, "21:00"), CancellationToken.None);

        var autre = _carnet.Context.Rappels.Single(r => r.CarnetSanteId == CarnetDeTest.AutreCarnetSanteId);
        Assert.False(autre.Actif);
        Assert.Equal(new TimeOnly(8, 0), autre.Heure);
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
        Assert.IsType<UnauthorizedResult>(await controller.GetRappels(CancellationToken.None));
        Assert.IsType<UnauthorizedResult>(await controller.PutRappel("BilanQuotidien", new RappelDto(), CancellationToken.None));
        Assert.IsType<UnauthorizedResult>(await controller.EnvoyerTest(CancellationToken.None));
    }

    public void Dispose() => _carnet.Dispose();
}
