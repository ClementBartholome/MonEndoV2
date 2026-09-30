using Microsoft.Extensions.Logging.Abstractions;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services.WebPush;
using MonEndoVue.Server.Services.WebPush.Rappels;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

public sealed class NotificationsPushServiceTests : IDisposable
{
    // Samedi 26/09/2026 19:30 UTC = 21:30 à Paris (heure d'été, UTC+2).
    private static readonly DateTimeOffset Maintenant = new(2026, 9, 26, 19, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly AujourdhuiParis = new(2026, 9, 26);

    private readonly CarnetDeTest _carnet = new();
    private readonly FauxEnvoiPush _envoi = new();

    private NotificationsPushService Service(DateTimeOffset? maintenant = null) => new(
        _carnet.Context,
        _envoi,
        [new RappelBilanQuotidien(_carnet.Context), new RappelSuiviAcne(_carnet.Context)],
        new HorlogeFixe(maintenant ?? Maintenant),
        NullLogger<NotificationsPushService>.Instance);

    private Rappel AjouterRappel(TypeRappel type, TimeOnly heure, DayOfWeek? jour = null, bool actif = true,
        string fuseau = "Europe/Paris", DateOnly? dernierEnvoi = null)
    {
        var rappel = new Rappel
        {
            CarnetSanteId = CarnetDeTest.CarnetSanteId,
            Type = type,
            Actif = actif,
            Heure = heure,
            JourSemaine = jour,
            FuseauHoraire = fuseau,
            DernierEnvoiLe = dernierEnvoi,
        };
        _carnet.Context.Rappels.Add(rappel);
        _carnet.Context.SaveChanges();
        return rappel;
    }

    private void Abonnement(int carnetSanteId = CarnetDeTest.CarnetSanteId, string endpoint = "https://push.example/a")
    {
        _carnet.Context.AbonnementsPush.Add(PushDeTest.Abonnement(carnetSanteId, endpoint));
        _carnet.Context.SaveChanges();
    }

    [Fact]
    public async Task EnvoyerAuCarnet_EnvoieAuxSeulsAppareilsDuCarnetEtSupprimeLesExpires()
    {
        Abonnement(endpoint: "https://push.example/actif");
        Abonnement(endpoint: "https://push.example/expire");
        Abonnement(CarnetDeTest.AutreCarnetSanteId, "https://push.example/autre");
        _envoi.ResultatsParEndpoint["https://push.example/expire"] = ResultatEnvoiPush.AbonnementExpire;

        var envoyes = await Service().EnvoyerAuCarnetAsync(
            CarnetDeTest.CarnetSanteId, new MessagePush("MonEndo", "Test", "/"), CancellationToken.None);

        Assert.Equal(1, envoyes);
        Assert.DoesNotContain(_envoi.Envois, e => e.Abonnement.CarnetSanteId == CarnetDeTest.AutreCarnetSanteId);
        Assert.DoesNotContain(_carnet.Context.AbonnementsPush, a => a.Endpoint == "https://push.example/expire");
        Assert.Equal(2, _carnet.Context.AbonnementsPush.Count());
    }

    [Fact]
    public async Task Rappels_BilanDu_EnvoieUneSeuleFoisVersLaPageBilan()
    {
        var rappel = AjouterRappel(TypeRappel.BilanQuotidien, new TimeOnly(21, 0));
        Abonnement();

        Assert.Equal(1, await Service().EnvoyerRappelsDusAsync(CancellationToken.None));
        Assert.Equal(0, await Service().EnvoyerRappelsDusAsync(CancellationToken.None));

        var envoi = Assert.Single(_envoi.Envois);
        Assert.Equal("/bilan-quotidien?ajouter", envoi.Message.Url);
        Assert.Equal(AujourdhuiParis, rappel.DernierEnvoiLe);
    }

    [Fact]
    public async Task Rappels_HeureLocaleNonAtteinte_NEnvoieRien()
    {
        AjouterRappel(TypeRappel.BilanQuotidien, new TimeOnly(22, 0)); // 21:30 à Paris
        Abonnement();

        Assert.Equal(0, await Service().EnvoyerRappelsDusAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Rappels_UtiliseLeFuseauDeLUtilisatrice()
    {
        AjouterRappel(TypeRappel.BilanQuotidien, new TimeOnly(15, 0), fuseau: "America/New_York"); // 15:30 à New York
        Abonnement();

        Assert.Equal(1, await Service().EnvoyerRappelsDusAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Rappels_DejaEnvoyeAujourdhui_NEnvoieRien()
    {
        AjouterRappel(TypeRappel.BilanQuotidien, new TimeOnly(21, 0), dernierEnvoi: AujourdhuiParis);
        Abonnement();

        Assert.Equal(0, await Service().EnvoyerRappelsDusAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Rappels_BilanDejaRempli_NEnvoieRien()
    {
        AjouterRappel(TypeRappel.BilanQuotidien, new TimeOnly(21, 0));
        Abonnement();
        _carnet.Context.BilansQuotidiens.Add(new BilanQuotidien
        {
            CarnetSanteId = CarnetDeTest.CarnetSanteId,
            Date = new DateTime(2026, 9, 26, 8, 15, 0),
            Mood = "Neutre",
        });
        _carnet.Context.SaveChanges();

        Assert.Equal(0, await Service().EnvoyerRappelsDusAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Rappels_AcneLeBonJour_EnvoieVersLOngletAcne()
    {
        AjouterRappel(TypeRappel.SuiviAcne, new TimeOnly(20, 0), DayOfWeek.Saturday);
        Abonnement();

        Assert.Equal(1, await Service().EnvoyerRappelsDusAsync(CancellationToken.None));
        Assert.Equal("/cycle?onglet=acne", Assert.Single(_envoi.Envois).Message.Url);
    }

    [Fact]
    public async Task Rappels_AcneUnAutreJour_NEnvoieRien()
    {
        AjouterRappel(TypeRappel.SuiviAcne, new TimeOnly(20, 0), DayOfWeek.Sunday); // aujourd'hui samedi
        Abonnement();

        Assert.Equal(0, await Service().EnvoyerRappelsDusAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Rappels_RappelDesactiveOuFuseauInconnu_NEnvoieRien()
    {
        var rappel = AjouterRappel(TypeRappel.BilanQuotidien, new TimeOnly(8, 0), actif: false);
        Abonnement();
        var service = Service();

        Assert.Equal(0, await service.EnvoyerRappelsDusAsync(CancellationToken.None));

        rappel.Actif = true;
        rappel.FuseauHoraire = "Fuseau/Inexistant";
        _carnet.Context.SaveChanges();

        Assert.Equal(0, await service.EnvoyerRappelsDusAsync(CancellationToken.None));
        Assert.Empty(_envoi.Envois);
    }

    [Fact]
    public async Task Rappels_AucunAppareilAbonne_NeMarquePasLeRappelCommeEnvoye()
    {
        var rappel = AjouterRappel(TypeRappel.BilanQuotidien, new TimeOnly(21, 0));

        Assert.Equal(0, await Service().EnvoyerRappelsDusAsync(CancellationToken.None));
        Assert.Null(rappel.DernierEnvoiLe);
    }

    [Fact]
    public async Task Rappels_TypeSansRegleEnregistree_EstIgnore()
    {
        AjouterRappel(TypeRappel.SuiviAcne, new TimeOnly(20, 0), DayOfWeek.Saturday);
        Abonnement();
        var serviceSansRegleAcne = new NotificationsPushService(
            _carnet.Context, _envoi, [new RappelBilanQuotidien(_carnet.Context)],
            new HorlogeFixe(Maintenant), NullLogger<NotificationsPushService>.Instance);

        Assert.Equal(0, await serviceSansRegleAcne.EnvoyerRappelsDusAsync(CancellationToken.None));
    }

    public void Dispose() => _carnet.Dispose();
}
