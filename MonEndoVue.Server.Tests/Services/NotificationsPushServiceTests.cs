using Microsoft.Extensions.Logging.Abstractions;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services.WebPush;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

public sealed class NotificationsPushServiceTests : IDisposable
{
    // 26/09/2026 19:30 UTC = 21:30 à Paris (heure d'été, UTC+2).
    private static readonly DateTimeOffset Maintenant = new(2026, 9, 26, 19, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly AujourdhuiParis = new(2026, 9, 26);

    private readonly CarnetDeTest _carnet = new();
    private readonly FauxEnvoiPush _envoi = new();

    private NotificationsPushService Service(DateTimeOffset? maintenant = null) => new(
        _carnet.Context, _envoi, new HorlogeFixe(maintenant ?? Maintenant), NullLogger<NotificationsPushService>.Instance);

    private void Preference(TimeOnly heure, bool actif = true, string fuseau = "Europe/Paris", DateOnly? dernierRappel = null)
    {
        _carnet.Context.PreferencesRappel.Add(new PreferenceRappel
        {
            CarnetSanteId = CarnetDeTest.CarnetSanteId,
            RappelActif = actif,
            HeureRappel = heure,
            FuseauHoraire = fuseau,
            DernierRappelLe = dernierRappel,
        });
        _carnet.Context.SaveChanges();
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
            CarnetDeTest.CarnetSanteId, NotificationsPushService.MessageRappelBilan, CancellationToken.None);

        Assert.Equal(1, envoyes);
        Assert.DoesNotContain(_envoi.Envois, e => e.Abonnement.CarnetSanteId == CarnetDeTest.AutreCarnetSanteId);
        Assert.DoesNotContain(_carnet.Context.AbonnementsPush, a => a.Endpoint == "https://push.example/expire");
        Assert.Equal(2, _carnet.Context.AbonnementsPush.Count());
    }

    [Fact]
    public async Task Rappels_HeureLocaleAtteinteEtBilanNonRempli_EnvoieLeRappelUneSeuleFois()
    {
        Preference(new TimeOnly(21, 0));
        Abonnement();

        var premierPassage = await Service().EnvoyerRappelsDusAsync(CancellationToken.None);
        var secondPassage = await Service().EnvoyerRappelsDusAsync(CancellationToken.None);

        Assert.Equal(1, premierPassage);
        Assert.Equal(0, secondPassage);
        var envoi = Assert.Single(_envoi.Envois);
        Assert.Equal(NotificationsPushService.MessageRappelBilan, envoi.Message);
        Assert.Equal(AujourdhuiParis, _carnet.Context.PreferencesRappel.Single().DernierRappelLe);
    }

    [Fact]
    public async Task Rappels_HeureLocaleNonAtteinte_NEnvoieRien()
    {
        Preference(new TimeOnly(22, 0)); // 21:30 à Paris
        Abonnement();

        Assert.Equal(0, await Service().EnvoyerRappelsDusAsync(CancellationToken.None));
        Assert.Empty(_envoi.Envois);
    }

    [Fact]
    public async Task Rappels_UtiliseLeFuseauDeLUtilisatrice()
    {
        Preference(new TimeOnly(15, 0), fuseau: "America/New_York"); // 15:30 à New York
        Abonnement();

        Assert.Equal(1, await Service().EnvoyerRappelsDusAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Rappels_DejaEnvoyeAujourdhui_NEnvoieRien()
    {
        Preference(new TimeOnly(21, 0), dernierRappel: AujourdhuiParis);
        Abonnement();

        Assert.Equal(0, await Service().EnvoyerRappelsDusAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData("2026-09-25T22:00:00")] // minuit à Paris, enregistré en UTC la veille
    [InlineData("2026-09-26T08:15:00")]
    public async Task Rappels_BilanDuJourLocalDejaRempli_NEnvoieRien(string dateBilanUtc)
    {
        Preference(new TimeOnly(21, 0));
        Abonnement();
        _carnet.Context.BilansQuotidiens.Add(new BilanQuotidien
        {
            CarnetSanteId = CarnetDeTest.CarnetSanteId,
            Date = DateTime.Parse(dateBilanUtc, System.Globalization.CultureInfo.InvariantCulture),
            Mood = "Neutre",
        });
        _carnet.Context.SaveChanges();

        Assert.Equal(0, await Service().EnvoyerRappelsDusAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Rappels_BilanDeLaVeilleSeulement_EnvoieLeRappel()
    {
        Preference(new TimeOnly(21, 0));
        Abonnement();
        _carnet.Context.BilansQuotidiens.Add(new BilanQuotidien
        {
            CarnetSanteId = CarnetDeTest.CarnetSanteId,
            Date = new DateTime(2026, 9, 25, 21, 59, 0),
            Mood = "Neutre",
        });
        _carnet.Context.SaveChanges();

        Assert.Equal(1, await Service().EnvoyerRappelsDusAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Rappels_RappelDesactiveOuFuseauInconnu_NEnvoieRien()
    {
        Preference(new TimeOnly(8, 0), actif: false);
        Abonnement();
        var service = Service();

        Assert.Equal(0, await service.EnvoyerRappelsDusAsync(CancellationToken.None));

        var preference = _carnet.Context.PreferencesRappel.Single();
        preference.RappelActif = true;
        preference.FuseauHoraire = "Fuseau/Inexistant";
        _carnet.Context.SaveChanges();

        Assert.Equal(0, await service.EnvoyerRappelsDusAsync(CancellationToken.None));
        Assert.Empty(_envoi.Envois);
    }

    [Fact]
    public async Task Rappels_AucunAppareilAbonne_NeMarquePasLeRappelCommeEnvoye()
    {
        Preference(new TimeOnly(21, 0));

        Assert.Equal(0, await Service().EnvoyerRappelsDusAsync(CancellationToken.None));
        Assert.Null(_carnet.Context.PreferencesRappel.Single().DernierRappelLe);
    }

    [Fact]
    public void JourneeEnUtc_JourneeParisienneEnHeureDEte_CommenceLaVeilleA22hUtc()
    {
        var fuseau = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

        var (debut, fin) = NotificationsPushService.JourneeEnUtc(AujourdhuiParis, fuseau);

        Assert.Equal(new DateTime(2026, 9, 25, 22, 0, 0), debut);
        Assert.Equal(new DateTime(2026, 9, 26, 22, 0, 0), fin);
    }

    [Theory]
    [InlineData("Europe/Paris", true)]
    [InlineData("America/New_York", true)]
    [InlineData("Fuseau/Inexistant", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void EstFuseauValide_ReconnaitLesFuseauxIana(string? fuseau, bool attendu)
    {
        Assert.Equal(attendu, NotificationsPushService.EstFuseauValide(fuseau));
    }

    public void Dispose() => _carnet.Dispose();
}
