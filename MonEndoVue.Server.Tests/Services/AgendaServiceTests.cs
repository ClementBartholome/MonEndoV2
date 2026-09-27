using System.Net;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Agenda;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

public class AgendaServiceTests
{
    private static readonly DateTimeOffset Debut = new(2026, 9, 1, 0, 0, 0, TimeSpan.FromHours(2));
    private static readonly DateTimeOffset Fin = new(2026, 10, 1, 0, 0, 0, TimeSpan.FromHours(2));

    private static FauxGoogleCalendar Google(string items = "") => new(_ => AgendaDeTest.Reponse(items));

    [Fact]
    public async Task GetEvenementsAsync_UtilisatriceSansCalendrier_IntrouvableSansAppelerGoogle()
    {
        var google = Google();
        var service = AgendaDeTest.Service(google);

        var resultat = await service.GetEvenementsAsync(CarnetDeTest.AutreUserId, Debut, Fin, CancellationToken.None);

        Assert.Equal(StatutOperation.Introuvable, resultat.Statut);
        Assert.Empty(google.Requetes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetEvenementsAsync_SansCleApi_Introuvable(string? cleApi)
    {
        var google = Google();
        var service = AgendaDeTest.Service(google, AgendaDeTest.Options(cleApi));

        var resultat = await service.GetEvenementsAsync(CarnetDeTest.UserId, Debut, Fin, CancellationToken.None);

        Assert.Equal(StatutOperation.Introuvable, resultat.Statut);
        Assert.Empty(google.Requetes);
    }

    [Fact]
    public async Task GetEvenementsAsync_IdentifiantEnMajuscules_TrouveLeCalendrier()
    {
        var service = AgendaDeTest.Service(Google(), AgendaDeTest.Options(userId: CarnetDeTest.UserId.ToUpperInvariant()));

        var resultat = await service.GetEvenementsAsync(CarnetDeTest.UserId, Debut, Fin, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(63)]
    public async Task GetEvenementsAsync_PeriodeInvalide_Invalide(int jours)
    {
        var google = Google();
        var service = AgendaDeTest.Service(google);

        var resultat = await service.GetEvenementsAsync(CarnetDeTest.UserId, Debut, Debut.AddDays(jours), CancellationToken.None);

        Assert.Equal(StatutOperation.Invalide, resultat.Statut);
        Assert.Empty(google.Requetes);
    }

    [Fact]
    public async Task GetEvenementsAsync_Periode_InterrogeLeCalendrierDeLUtilisatriceAvecLaCleServeur()
    {
        var google = Google();
        var service = AgendaDeTest.Service(google);

        await service.GetEvenementsAsync(CarnetDeTest.UserId, Debut, Fin, CancellationToken.None);

        var requete = Assert.Single(google.Requetes);
        var url = requete.RequestUri!.AbsoluteUri;
        Assert.Equal(AgendaDeTest.CleApi, Assert.Single(requete.Headers.GetValues(AgendaService.EnteteCleApi)));
        Assert.DoesNotContain(AgendaDeTest.CleApi, url);
        Assert.StartsWith($"{AgendaService.UrlApi}agenda.test%40example.com/events?", url);
        Assert.Contains("timeMin=2026-08-31T22%3A00%3A00Z", url);
        Assert.Contains("timeMax=2026-09-30T22%3A00%3A00Z", url);
        Assert.Contains("singleEvents=true", url);
        Assert.Contains("orderBy=startTime", url);
    }

    [Fact]
    public async Task GetEvenementsAsync_ReponseGoogle_GardeLesChampsAffichesEtEcarteLesEvenementsSansTitre()
    {
        var items = string.Join(',',
            AgendaDeTest.Evenement("rdv", "Gynécologue", "2026-09-10T09:30:00+02:00", lieu: "Cabinet"),
            AgendaDeTest.Evenement("conges", "Congés", "2026-09-12", journeeEntiere: true, lieu: "  "),
            AgendaDeTest.Evenement("sans-titre", null, "2026-09-13T10:00:00+02:00"));
        var service = AgendaDeTest.Service(Google(items));

        var resultat = await service.GetEvenementsAsync(CarnetDeTest.UserId, Debut, Fin, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        Assert.Collection(resultat.Valeur!,
            rdv =>
            {
                Assert.Equal("rdv", rdv.Id);
                Assert.Equal("Gynécologue", rdv.Titre);
                Assert.Equal("2026-09-10T09:30:00+02:00", rdv.Debut);
                Assert.Equal("2026-09-10T09:30:00+02:00", rdv.Fin);
                Assert.False(rdv.JourneeEntiere);
                Assert.Equal("Cabinet", rdv.Lieu);
                Assert.Equal("https://calendar.example/rdv", rdv.Lien);
            },
            conges =>
            {
                Assert.Equal("2026-09-12", conges.Debut);
                Assert.True(conges.JourneeEntiere);
                Assert.Null(conges.Lieu);
            });
    }

    [Fact]
    public async Task GetEvenementsAsync_PlusieursPages_LitLaPageSuivante()
    {
        var google = new FauxGoogleCalendar(requete => requete.RequestUri!.Query.Contains("pageToken=page-2")
            ? AgendaDeTest.Reponse(AgendaDeTest.Evenement("b", "B", "2026-09-20T10:00:00Z"))
            : AgendaDeTest.Reponse(AgendaDeTest.Evenement("a", "A", "2026-09-10T10:00:00Z"), "page-2"));
        var service = AgendaDeTest.Service(google);

        var resultat = await service.GetEvenementsAsync(CarnetDeTest.UserId, Debut, Fin, CancellationToken.None);

        Assert.Equal(["a", "b"], resultat.Valeur!.Select(e => e.Id));
        Assert.Equal(2, google.Requetes.Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetEvenementsAsync_ErreurGoogle_Indisponible(HttpStatusCode statut)
    {
        var service = AgendaDeTest.Service(new FauxGoogleCalendar(_ => new HttpResponseMessage(statut)));

        var resultat = await service.GetEvenementsAsync(CarnetDeTest.UserId, Debut, Fin, CancellationToken.None);

        Assert.Equal(StatutOperation.Indisponible, resultat.Statut);
        Assert.NotNull(resultat.Message);
    }

    [Fact]
    public async Task GetEvenementsAsync_GoogleInjoignable_Indisponible()
    {
        var service = AgendaDeTest.Service(new FauxGoogleCalendar(_ => throw new HttpRequestException("réseau")));

        var resultat = await service.GetEvenementsAsync(CarnetDeTest.UserId, Debut, Fin, CancellationToken.None);

        Assert.Equal(StatutOperation.Indisponible, resultat.Statut);
    }

    [Fact]
    public async Task GetEvenementsAsync_ReponseIllisible_Indisponible()
    {
        var service = AgendaDeTest.Service(new FauxGoogleCalendar(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("pas du json", System.Text.Encoding.UTF8, "application/json"),
        }));

        var resultat = await service.GetEvenementsAsync(CarnetDeTest.UserId, Debut, Fin, CancellationToken.None);

        Assert.Equal(StatutOperation.Indisponible, resultat.Statut);
    }

    [Fact]
    public async Task GetEvenementsAsync_AnnulationDeLaRequete_PropageLAnnulation()
    {
        using var annulation = new CancellationTokenSource();
        var service = AgendaDeTest.Service(new FauxGoogleCalendar(_ =>
        {
            annulation.Cancel();
            throw new TaskCanceledException();
        }));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.GetEvenementsAsync(CarnetDeTest.UserId, Debut, Fin, annulation.Token));
    }

    [Fact]
    public async Task GetProchainsAsync_UtilisatriceSansCalendrier_Introuvable()
    {
        var google = Google();
        var service = AgendaDeTest.Service(google);

        var resultat = await service.GetProchainsAsync(CarnetDeTest.AutreUserId, CancellationToken.None);

        Assert.Equal(StatutOperation.Introuvable, resultat.Statut);
        Assert.Empty(google.Requetes);
    }

    [Fact]
    public async Task GetProchainsAsync_EvenementsAVenir_TroisPremiersAHeureFixeAPartirDeMaintenant()
    {
        var items = string.Join(',',
            AgendaDeTest.Evenement("journee", "Journée", "2026-09-28", journeeEntiere: true),
            AgendaDeTest.Evenement("1", "Un", "2026-09-28T09:00:00+02:00"),
            AgendaDeTest.Evenement("2", "Deux", "2026-09-29T09:00:00+02:00"),
            AgendaDeTest.Evenement("3", "Trois", "2026-09-30T09:00:00+02:00"),
            AgendaDeTest.Evenement("4", "Quatre", "2026-10-01T09:00:00+02:00"));
        var google = Google(items);
        var service = AgendaDeTest.Service(google);

        var resultat = await service.GetProchainsAsync(CarnetDeTest.UserId, CancellationToken.None);

        Assert.Equal(["1", "2", "3"], resultat.Valeur!.Select(e => e.Id));
        var url = Assert.Single(google.Requetes).RequestUri!.AbsoluteUri;
        Assert.Contains("timeMin=2026-09-27T10%3A00%3A00Z", url);
        Assert.DoesNotContain("timeMax", url);
    }

    [Fact]
    public async Task GetProchainsAsync_ErreurGoogle_Indisponible()
    {
        var service = AgendaDeTest.Service(new FauxGoogleCalendar(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)));

        var resultat = await service.GetProchainsAsync(CarnetDeTest.UserId, CancellationToken.None);

        Assert.Equal(StatutOperation.Indisponible, resultat.Statut);
    }
}
