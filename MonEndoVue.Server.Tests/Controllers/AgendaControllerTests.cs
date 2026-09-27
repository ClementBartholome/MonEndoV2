using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.ViewModels;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Controllers;

public class AgendaControllerTests
{
    private static readonly DateTimeOffset Debut = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    private static AgendaController Controller(FauxGoogleCalendar google, bool authentifie = true) =>
        new(AgendaDeTest.Service(google))
        {
            ControllerContext = authentifie ? CarnetDeTest.ContexteAuthentifie() : CarnetDeTest.ContexteAnonyme(),
        };

    private static FauxGoogleCalendar Google() =>
        new(_ => AgendaDeTest.Reponse(AgendaDeTest.Evenement("rdv", "Rendez-vous", "2026-09-28T09:00:00+02:00")));

    private static int? StatutDe(IActionResult resultat) => (resultat as IStatusCodeActionResult)?.StatusCode;

    [Fact]
    public async Task GetEvenements_UtilisatriceAvecAgenda_RenvoieLesEvenements()
    {
        var resultat = await Controller(Google()).GetEvenements(Debut, Debut.AddDays(31), CancellationToken.None);

        var evenements = Assert.IsAssignableFrom<IReadOnlyList<EvenementAgendaViewModel>>(
            Assert.IsType<OkObjectResult>(resultat).Value);
        Assert.Equal("rdv", Assert.Single(evenements).Id);
    }

    [Fact]
    public async Task GetEvenements_SansUtilisatriceConnectee_NAppellePasGoogle()
    {
        var google = Google();

        var resultat = await Controller(google, authentifie: false).GetEvenements(Debut, Debut.AddDays(31), CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultat);
        Assert.Empty(google.Requetes);
    }

    [Fact]
    public async Task GetEvenements_PeriodeTropLongue_BadRequest()
    {
        var resultat = await Controller(Google()).GetEvenements(Debut, Debut.AddYears(1), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultat);
    }

    [Fact]
    public async Task GetProchains_UtilisatriceAvecAgenda_RenvoieLesProchainsRendezVous()
    {
        var resultat = await Controller(Google()).GetProchains(CancellationToken.None);

        var evenements = Assert.IsAssignableFrom<IReadOnlyList<EvenementAgendaViewModel>>(
            Assert.IsType<OkObjectResult>(resultat).Value);
        Assert.Single(evenements);
    }

    [Fact]
    public async Task GetProchains_GoogleIndisponible_503()
    {
        var google = new FauxGoogleCalendar(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var resultat = await Controller(google).GetProchains(CancellationToken.None);

        Assert.Equal(503, StatutDe(resultat));
    }
}
