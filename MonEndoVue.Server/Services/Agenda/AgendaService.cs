using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Services.Agenda;

/// <summary>
/// Lecture de l'agenda Google de l'utilisatrice connectée, via l'API Calendar et une clé restée côté serveur.
/// Le calendrier est déduit de la session (configuration), jamais reçu du client.
/// </summary>
public class AgendaService(
    HttpClient httpClient,
    IOptions<AgendaOptions> options,
    TimeProvider horloge,
    ILogger<AgendaService> logger)
{
    public const string UrlApi = "https://www.googleapis.com/calendar/v3/calendars/";
    public const string EnteteCleApi = "X-goog-api-key";

    /// <summary>Une vue mois de FullCalendar couvre au plus 6 semaines : au-delà, la demande est refusée.</summary>
    public static readonly TimeSpan PeriodeMax = TimeSpan.FromDays(62);

    public const int NombreProchains = 3;
    private const int ProchainsCandidats = 20; // les événements sur la journée entière sont écartés ensuite
    private const int EvenementsParPage = 250;
    private const int PagesMax = 4;
    private const string MessageIndisponible = "L'agenda est momentanément indisponible.";

    public async Task<ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>> GetEvenementsAsync(
        string userId, DateTimeOffset debut, DateTimeOffset fin, CancellationToken cancellationToken)
    {
        var calendrier = options.Value.CalendrierDe(userId);
        if (calendrier is null)
        {
            return ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>.Echec(StatutOperation.Introuvable);
        }

        if (fin <= debut || fin - debut > PeriodeMax)
        {
            return ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>.Echec(
                StatutOperation.Invalide, "La période demandée est invalide.");
        }

        return await LireAsync(calendrier, debut, fin, EvenementsParPage, PagesMax, cancellationToken);
    }

    /// <summary>Les prochains rendez-vous à heure fixe (sans les événements sur la journée entière), comme sur l'accueil.</summary>
    public async Task<ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>> GetProchainsAsync(
        string userId, CancellationToken cancellationToken)
    {
        var calendrier = options.Value.CalendrierDe(userId);
        if (calendrier is null)
        {
            return ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>.Echec(StatutOperation.Introuvable);
        }

        var resultat = await LireAsync(calendrier, horloge.GetUtcNow(), null, ProchainsCandidats, 1, cancellationToken);
        return resultat.Statut != StatutOperation.Succes
            ? resultat
            : ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>.Succes(
                resultat.Valeur!.Where(e => !e.JourneeEntiere).Take(NombreProchains).ToList());
    }

    private async Task<ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>> LireAsync(
        string calendrier, DateTimeOffset debut, DateTimeOffset? fin, int parPage, int pagesMax,
        CancellationToken cancellationToken)
    {
        var evenements = new List<EvenementAgendaViewModel>();
        string? pageSuivante = null;
        try
        {
            for (var page = 0; page < pagesMax; page++)
            {
                using var requete = new HttpRequestMessage(HttpMethod.Get, Url(calendrier, debut, fin, parPage, pageSuivante));
                // Clé en en-tête plutôt qu'en query string : HttpClient journalise les URL (niveau Information).
                requete.Headers.Add(EnteteCleApi, options.Value.CleApi);
                using var reponse = await httpClient.SendAsync(requete, cancellationToken);
                if (!reponse.IsSuccessStatusCode)
                {
                    // Ni le calendrier ni la clé dans les logs : le code HTTP suffit au diagnostic.
                    logger.LogWarning("Google Calendar request failed with status {StatusCode}", (int)reponse.StatusCode);
                    return ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>.Echec(
                        StatutOperation.Indisponible, MessageIndisponible);
                }

                var contenu = await reponse.Content.ReadFromJsonAsync<ReponseGoogle>(cancellationToken);
                evenements.AddRange((contenu?.Items ?? []).Select(VersViewModel).OfType<EvenementAgendaViewModel>());
                pageSuivante = contenu?.NextPageToken;
                if (string.IsNullOrEmpty(pageSuivante))
                {
                    break;
                }
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            logger.LogWarning("Google Calendar request failed ({ExceptionType})", ex.GetType().Name);
            return ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>.Echec(
                StatutOperation.Indisponible, MessageIndisponible);
        }

        return ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>.Succes(evenements);
    }

    private string Url(string calendrier, DateTimeOffset debut, DateTimeOffset? fin, int parPage, string? pageSuivante)
    {
        var parametres = new List<string>
        {
            $"timeMin={Uri.EscapeDataString(Rfc3339(debut))}",
            "singleEvents=true",
            "orderBy=startTime",
            $"maxResults={parPage}",
        };
        if (fin is not null)
        {
            parametres.Add($"timeMax={Uri.EscapeDataString(Rfc3339(fin.Value))}");
        }

        if (!string.IsNullOrEmpty(pageSuivante))
        {
            parametres.Add($"pageToken={Uri.EscapeDataString(pageSuivante)}");
        }

        return $"{UrlApi}{Uri.EscapeDataString(calendrier)}/events?{string.Join('&', parametres)}";
    }

    private static string Rfc3339(DateTimeOffset date) =>
        date.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    /// <summary>Un événement sans titre ni date de début n'est pas affiché (comportement de l'ancien agenda).</summary>
    private static EvenementAgendaViewModel? VersViewModel(EvenementGoogle e)
    {
        var debut = e.Start?.DateTime ?? e.Start?.Date;
        if (string.IsNullOrWhiteSpace(e.Summary) || string.IsNullOrEmpty(debut))
        {
            return null;
        }

        return new EvenementAgendaViewModel(
            e.Id ?? string.Empty,
            e.Summary,
            debut,
            e.End?.DateTime ?? e.End?.Date,
            e.Start?.DateTime is null,
            string.IsNullOrWhiteSpace(e.Location) ? null : e.Location,
            e.HtmlLink);
    }

    private sealed record ReponseGoogle(
        [property: JsonPropertyName("items")] List<EvenementGoogle>? Items,
        [property: JsonPropertyName("nextPageToken")] string? NextPageToken);

    private sealed record EvenementGoogle(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("summary")] string? Summary,
        [property: JsonPropertyName("location")] string? Location,
        [property: JsonPropertyName("htmlLink")] string? HtmlLink,
        [property: JsonPropertyName("start")] DateGoogle? Start,
        [property: JsonPropertyName("end")] DateGoogle? End);

    private sealed record DateGoogle(
        [property: JsonPropertyName("dateTime")] string? DateTime,
        [property: JsonPropertyName("date")] string? Date);
}
