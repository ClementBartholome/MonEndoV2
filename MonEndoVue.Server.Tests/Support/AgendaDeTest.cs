using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MonEndoVue.Server.Services.Agenda;

namespace MonEndoVue.Server.Tests.Support;

/// <summary>Outils de test de l'agenda : faux service Google Calendar et configuration.</summary>
public static class AgendaDeTest
{
    public const string Calendrier = "agenda.test@example.com";
    public const string CleApi = "cle-api-test";
    public static readonly DateTimeOffset Maintenant = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);

    public static IOptions<AgendaOptions> Options(string? cleApi = CleApi, string userId = CarnetDeTest.UserId) =>
        Microsoft.Extensions.Options.Options.Create(new AgendaOptions
        {
            CleApi = cleApi,
            Calendriers = new Dictionary<string, string> { [userId] = Calendrier },
        });

    /// <summary>Sans liaison fournie, une base vide : aucune utilisatrice liée, seule la configuration compte.</summary>
    public static AgendaService Service(
        FauxGoogleCalendar google, IOptions<AgendaOptions>? options = null, LiaisonAgendaService? liaison = null) =>
        new(new HttpClient(google), options ?? Options(),
            liaison ?? LiaisonAgendaDeTest.ServiceSansGoogle(new CarnetDeTest().Context),
            new HorlogeFixe(Maintenant), NullLogger<AgendaService>.Instance);

    /// <summary>Réponse JSON de l'API Google Calendar (format events.list).</summary>
    public static HttpResponseMessage Reponse(string items, string? pageSuivante = null) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $"{{\"items\":[{items}]{(pageSuivante is null ? "" : $",\"nextPageToken\":\"{pageSuivante}\"")}}}",
                Encoding.UTF8,
                "application/json"),
        };

    /// <summary>Réponse de la liste des calendriers (calendarList.list) ; chaque entrée : identifiant, nom, principal ou non.</summary>
    public static HttpResponseMessage ReponseCalendriers(params (string Id, string Nom, bool Principal)[] calendriers) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"items\":[" + string.Join(',', calendriers.Select(c =>
                    $"{{\"id\":\"{c.Id}\",\"summary\":\"{c.Nom}\"" + (c.Principal ? ",\"primary\":true" : "") + "}")) + "]}",
                Encoding.UTF8,
                "application/json"),
        };

    public static string Evenement(string id, string? titre, string debut, bool journeeEntiere = false, string? lieu = null)
    {
        var cle = journeeEntiere ? "date" : "dateTime";
        var champTitre = titre is null ? "" : $"\"summary\":\"{titre}\",";
        var champLieu = lieu is null ? "" : $"\"location\":\"{lieu}\",";
        return $"{{\"id\":\"{id}\",{champTitre}{champLieu}\"htmlLink\":\"https://calendar.example/{id}\"," +
               $"\"start\":{{\"{cle}\":\"{debut}\"}},\"end\":{{\"{cle}\":\"{debut}\"}}}}";
    }
}

/// <summary>Faux service Google Calendar : enregistre les requêtes et répond selon la fonction fournie.</summary>
public sealed class FauxGoogleCalendar(Func<HttpRequestMessage, HttpResponseMessage> repondre) : HttpMessageHandler
{
    public List<HttpRequestMessage> Requetes { get; } = [];

    /// <summary>Corps (formulaire) de chaque requête, lu à l'envoi : le contenu est libéré ensuite.</summary>
    public List<string> Corps { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requetes.Add(request);
        Corps.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
        return repondre(request);
    }
}
