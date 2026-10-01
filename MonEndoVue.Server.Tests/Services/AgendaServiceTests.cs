using Microsoft.Extensions.Options;
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

    /// <summary>Compte lié par OAuth : la liaison et le service d'agenda partagent la même base.</summary>
    private const string CalendrierChoisi = "rdv.medicaux@example.com";

    private static async Task<(AgendaService Service, FauxGoogleCalendar Calendrier)> AvecLiaison(
        string items = "", IOptions<AgendaOptions>? configuration = null, bool calendrierChoisi = true,
        Func<HttpRequestMessage, HttpResponseMessage>? repondre = null)
    {
        var carnet = new CarnetDeTest();
        var protection = LiaisonAgendaDeTest.Protection();
        var liaison = LiaisonAgendaDeTest.Service(carnet.Context, LiaisonAgendaDeTest.FauxGoogle(), protection: protection);
        var debut = (await liaison.DemarrerAsync(CarnetDeTest.UserId, CancellationToken.None)).Valeur!;
        var etat = Uri.UnescapeDataString(debut.UrlAutorisation.Split("state=")[1].Split('&')[0]);
        await liaison.FinaliserAsync(debut.CookieEtat, etat, LiaisonAgendaDeTest.Code, null, CancellationToken.None);
        if (calendrierChoisi)
        {
            await liaison.EnregistrerCalendrierAsync(CarnetDeTest.UserId, CalendrierChoisi, CancellationToken.None);
        }

        var calendrier = repondre is null ? Google(items) : new FauxGoogleCalendar(repondre);
        return (AgendaDeTest.Service(calendrier, configuration, liaison), calendrier);
    }

    private static HttpResponseMessage CalendriersDeLUtilisatrice(HttpRequestMessage requete) =>
        requete.RequestUri!.AbsoluteUri.StartsWith(AgendaService.UrlListeCalendriers)
            ? AgendaDeTest.ReponseCalendriers(
                ("perso@example.com", "Perso", false), (CalendrierChoisi, "Rendez-vous médicaux", false), ("moi@example.com", "moi@example.com", true))
            : AgendaDeTest.Reponse("");

    [Fact]
    public async Task GetPrecedentAsync_RenvoieLeDernierRendezVousAHeureFixeAvantLaDate()
    {
        var items = string.Join(',',
            AgendaDeTest.Evenement("ancien", "Ancien", "2026-03-02T09:00:00+01:00"),
            AgendaDeTest.Evenement("dernier", "Dernier", "2026-06-12T11:00:00+02:00"),
            AgendaDeTest.Evenement("conges", "Congés", "2026-07-14", journeeEntiere: true));
        var (service, calendrier) = await AvecLiaison(items);
        var avant = new DateTimeOffset(2026, 10, 14, 14, 30, 0, TimeSpan.FromHours(2));

        var resultat = await service.GetPrecedentAsync(CarnetDeTest.UserId, avant, CancellationToken.None);

        Assert.Equal(["dernier"], resultat.Valeur!.Select(e => e.Id));
        var requete = Assert.Single(calendrier.Requetes);
        Assert.Contains($"{Uri.EscapeDataString(CalendrierChoisi)}/events?", requete.RequestUri!.AbsoluteUri);
        Assert.Contains("timeMax=2026-10-14T12%3A30%3A00Z", requete.RequestUri.AbsoluteUri);
    }

    [Fact]
    public async Task GetPrecedentAsync_AucunRendezVous_ListeVideSansErreur()
    {
        var (service, _) = await AvecLiaison();

        var resultat = await service.GetPrecedentAsync(CarnetDeTest.UserId, Fin, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        Assert.Empty(resultat.Valeur!);
    }

    [Fact]
    public async Task GetPrecedentAsync_SansCalendrierChoisiOuDateInvalide_NAppellePasGoogle()
    {
        var (sans, calendrierSans) = await AvecLiaison(calendrierChoisi: false);
        var (avec, calendrierAvec) = await AvecLiaison();

        var pasDeChoix = await sans.GetPrecedentAsync(CarnetDeTest.UserId, Fin, CancellationToken.None);
        var dateInvalide = await avec.GetPrecedentAsync(CarnetDeTest.UserId, default, CancellationToken.None);

        Assert.Equal(StatutOperation.Introuvable, pasDeChoix.Statut);
        Assert.Equal(StatutOperation.Invalide, dateInvalide.Statut);
        Assert.Empty(calendrierSans.Requetes);
        Assert.Empty(calendrierAvec.Requetes);
    }

    [Fact]
    public async Task GetEvenementsAsync_AgendaLieSansCalendrierChoisi_IntrouvableSansLireDEvenements()
    {
        var (service, calendrier) = await AvecLiaison(calendrierChoisi: false);

        var resultat = await service.GetEvenementsAsync(CarnetDeTest.UserId, Debut, Fin, CancellationToken.None);

        Assert.Equal(StatutOperation.Introuvable, resultat.Statut);
        Assert.Empty(calendrier.Requetes);
    }

    [Fact]
    public async Task GetProchainsAsync_AgendaLieSansCalendrierChoisi_IntrouvableMemeAvecUneEntreeDeConfiguration()
    {
        var (service, calendrier) = await AvecLiaison(configuration: AgendaDeTest.Options(), calendrierChoisi: false);

        var resultat = await service.GetProchainsAsync(CarnetDeTest.UserId, CancellationToken.None);

        Assert.Equal(StatutOperation.Introuvable, resultat.Statut);
        Assert.Empty(calendrier.Requetes);
    }

    [Fact]
    public async Task ListerCalendriersAsync_AgendaLie_RenvoieLePrincipalPuisLesAutresParNomAvecLeJeton()
    {
        var (service, calendrier) = await AvecLiaison(repondre: CalendriersDeLUtilisatrice);

        var resultat = await service.ListerCalendriersAsync(CarnetDeTest.UserId, CancellationToken.None);

        Assert.Equal(["moi@example.com", "perso@example.com", CalendrierChoisi], resultat.Valeur!.Select(c => c.Id));
        Assert.True(resultat.Valeur![0].Principal);
        var requete = Assert.Single(calendrier.Requetes);
        Assert.StartsWith(AgendaService.UrlListeCalendriers, requete.RequestUri!.AbsoluteUri);
        Assert.Contains("minAccessRole=reader", requete.RequestUri.AbsoluteUri);
        Assert.Equal(LiaisonAgendaDeTest.JetonAcces, requete.Headers.Authorization!.Parameter);
        Assert.DoesNotContain(LiaisonAgendaDeTest.JetonAcces, requete.RequestUri.AbsoluteUri);
    }

    [Fact]
    public async Task ListerCalendriersAsync_SansLiaison_IntrouvableSansAppelerGoogle()
    {
        var google = Google();
        var service = AgendaDeTest.Service(google);

        var resultat = await service.ListerCalendriersAsync(CarnetDeTest.UserId, CancellationToken.None);

        Assert.Equal(StatutOperation.Introuvable, resultat.Statut);
        Assert.Empty(google.Requetes);
    }

    [Fact]
    public async Task ListerCalendriersAsync_GoogleEnErreur_Indisponible()
    {
        var (service, _) = await AvecLiaison(repondre: _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var resultat = await service.ListerCalendriersAsync(CarnetDeTest.UserId, CancellationToken.None);

        Assert.Equal(StatutOperation.Indisponible, resultat.Statut);
    }

    [Fact]
    public async Task ChoisirCalendrierAsync_CalendrierDeLaListe_EstEnregistreEtLuEnsuite()
    {
        var (service, calendrier) = await AvecLiaison(calendrierChoisi: false, repondre: CalendriersDeLUtilisatrice);

        var resultat = await service.ChoisirCalendrierAsync(CarnetDeTest.UserId, "perso@example.com", CancellationToken.None);
        await service.GetEvenementsAsync(CarnetDeTest.UserId, Debut, Fin, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        Assert.StartsWith($"{AgendaService.UrlApi}{Uri.EscapeDataString("perso@example.com")}/events?", calendrier.Requetes[^1].RequestUri!.AbsoluteUri);
    }

    [Theory]
    [InlineData("calendrier-d-une-autre-personne@example.com")]
    [InlineData("primary")]
    [InlineData("")]
    public async Task ChoisirCalendrierAsync_IdentifiantAbsentDeLaListe_InvalideEtRienEnregistre(string identifiant)
    {
        var (service, calendrier) = await AvecLiaison(calendrierChoisi: false, repondre: CalendriersDeLUtilisatrice);

        var resultat = await service.ChoisirCalendrierAsync(CarnetDeTest.UserId, identifiant, CancellationToken.None);
        var evenements = await service.GetEvenementsAsync(CarnetDeTest.UserId, Debut, Fin, CancellationToken.None);

        Assert.Equal(StatutOperation.Invalide, resultat.Statut);
        Assert.Equal(StatutOperation.Introuvable, evenements.Statut);
        Assert.Single(calendrier.Requetes);
    }

    [Fact]
    public async Task ChoisirCalendrierAsync_GoogleEnPanne_IndisponibleEtRienEnregistre()
    {
        var (service, _) = await AvecLiaison(calendrierChoisi: false, repondre: _ => new HttpResponseMessage(HttpStatusCode.BadGateway));

        var resultat = await service.ChoisirCalendrierAsync(CarnetDeTest.UserId, CalendrierChoisi, CancellationToken.None);

        Assert.Equal(StatutOperation.Indisponible, resultat.Statut);
    }

    [Fact]
    public async Task GetEvenementsAsync_AgendaLie_LitLeCalendrierChoisiAvecLeJetonEtSansCleApi()
    {
        var (service, calendrier) = await AvecLiaison();

        var resultat = await service.GetEvenementsAsync(CarnetDeTest.UserId, Debut, Fin, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        var requete = Assert.Single(calendrier.Requetes);
        Assert.StartsWith($"{AgendaService.UrlApi}{Uri.EscapeDataString(CalendrierChoisi)}/events?", requete.RequestUri!.AbsoluteUri);
        Assert.Equal("Bearer", requete.Headers.Authorization!.Scheme);
        Assert.Equal(LiaisonAgendaDeTest.JetonAcces, requete.Headers.Authorization.Parameter);
        Assert.False(requete.Headers.Contains(AgendaService.EnteteCleApi));
        Assert.DoesNotContain(LiaisonAgendaDeTest.JetonAcces, requete.RequestUri.AbsoluteUri);
    }

    [Fact]
    public async Task GetEvenementsAsync_AgendaLieSansEntreeDeConfiguration_Fonctionne()
    {
        var (service, calendrier) = await AvecLiaison(configuration: AgendaDeTest.Options(cleApi: null));

        var resultat = await service.GetEvenementsAsync(CarnetDeTest.UserId, Debut, Fin, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        Assert.Single(calendrier.Requetes);
    }

    [Fact]
    public async Task GetProchainsAsync_AgendaLie_UtiliseLeJetonEtEcarteLesJourneesEntieres()
    {
        var items = string.Join(',',
            AgendaDeTest.Evenement("conges", "Congés", "2026-09-28", journeeEntiere: true),
            AgendaDeTest.Evenement("rdv", "Rendez-vous", "2026-09-29T09:00:00+02:00"));
        var (service, calendrier) = await AvecLiaison(items);

        var resultat = await service.GetProchainsAsync(CarnetDeTest.UserId, CancellationToken.None);

        Assert.Equal(["rdv"], resultat.Valeur!.Select(e => e.Id));
        Assert.Equal("Bearer", Assert.Single(calendrier.Requetes).Headers.Authorization!.Scheme);
    }

    [Fact]
    public async Task GetEvenementsAsync_AccordRevoque_RepliSurLaConfigurationPuisIntrouvableSansElle()
    {
        var carnet = new CarnetDeTest();
        var protection = LiaisonAgendaDeTest.Protection();
        var liaison = LiaisonAgendaDeTest.Service(carnet.Context, LiaisonAgendaDeTest.FauxGoogle(), protection: protection);
        var debut = (await liaison.DemarrerAsync(CarnetDeTest.UserId, CancellationToken.None)).Valeur!;
        var etat = Uri.UnescapeDataString(debut.UrlAutorisation.Split("state=")[1].Split('&')[0]);
        await liaison.FinaliserAsync(debut.CookieEtat, etat, LiaisonAgendaDeTest.Code, null, CancellationToken.None);
        var revoque = LiaisonAgendaDeTest.Service(
            carnet.Context, LiaisonAgendaDeTest.FauxGoogle(_ => LiaisonAgendaDeTest.Erreur("invalid_grant")), protection: protection);

        var avecConfig = await AgendaDeTest.Service(Google(), liaison: revoque)
            .GetEvenementsAsync(CarnetDeTest.UserId, Debut, Fin, CancellationToken.None);
        var sansConfig = await AgendaDeTest.Service(Google(), AgendaDeTest.Options(cleApi: null), revoque)
            .GetEvenementsAsync(CarnetDeTest.UserId, Debut, Fin, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, avecConfig.Statut);
        Assert.Equal(StatutOperation.Introuvable, sansConfig.Statut);
    }

    [Fact]
    public async Task GetEvenementsAsync_GoogleOAuthEnPanne_IndisponibleSansAppelerLeCalendrier()
    {
        var carnet = new CarnetDeTest();
        var protection = LiaisonAgendaDeTest.Protection();
        var liaison = LiaisonAgendaDeTest.Service(carnet.Context, LiaisonAgendaDeTest.FauxGoogle(), protection: protection);
        var debut = (await liaison.DemarrerAsync(CarnetDeTest.UserId, CancellationToken.None)).Valeur!;
        var etat = Uri.UnescapeDataString(debut.UrlAutorisation.Split("state=")[1].Split('&')[0]);
        await liaison.FinaliserAsync(debut.CookieEtat, etat, LiaisonAgendaDeTest.Code, null, CancellationToken.None);
        var enPanne = LiaisonAgendaDeTest.Service(
            carnet.Context, LiaisonAgendaDeTest.FauxGoogle(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)),
            protection: protection);
        var calendrier = Google();

        var resultat = await AgendaDeTest.Service(calendrier, liaison: enPanne)
            .GetEvenementsAsync(CarnetDeTest.UserId, Debut, Fin, CancellationToken.None);

        Assert.Equal(StatutOperation.Indisponible, resultat.Statut);
        Assert.Empty(calendrier.Requetes);
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
