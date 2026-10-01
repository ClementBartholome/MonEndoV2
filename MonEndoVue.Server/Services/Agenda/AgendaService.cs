using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Services.Agenda;

/// <summary>
/// Lecture de l'agenda Google de l'utilisatrice connectée, via l'API Calendar. Accès par la liaison OAuth de
/// l'utilisatrice (calendrier qu'elle a choisi, jeton d'accès) ou, à défaut et le temps de la transition, par la clé API et le
/// calendrier de la configuration. Le calendrier est déduit de la session, jamais reçu du client.
/// </summary>
public class AgendaService(
    HttpClient httpClient,
    IOptions<AgendaOptions> options,
    LiaisonAgendaService liaison,
    TimeProvider horloge,
    ILogger<AgendaService> logger)
{
    public const string UrlApi = "https://www.googleapis.com/calendar/v3/calendars/";
    public const string EnteteCleApi = "X-goog-api-key";
    public const string UrlListeCalendriers = "https://www.googleapis.com/calendar/v3/users/me/calendarList";

    /// <summary>Une vue mois de FullCalendar couvre au plus 6 semaines : au-delà, la demande est refusée.</summary>
    public static readonly TimeSpan PeriodeMax = TimeSpan.FromDays(62);

    /** Remontée maximale pour retrouver le rendez-vous précédent : la période la plus longue de l'export (un an). */
    public static readonly TimeSpan RemonteeRendezVousPrecedent = TimeSpan.FromDays(366);

    public const int NombreProchains = 3;
    private const int ProchainsCandidats = 20; // les événements sur la journée entière sont écartés ensuite
    private const int EvenementsParPage = 250;
    private const int PagesMax = 4;
    private const string MessageIndisponible = "L'agenda est momentanément indisponible.";

    public async Task<ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>> GetEvenementsAsync(
        string userId, DateTimeOffset debut, DateTimeOffset fin, CancellationToken cancellationToken)
    {
        var acces = await AccesAsync(userId, cancellationToken);
        if (acces.Statut != StatutOperation.Succes)
        {
            return ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>.Echec(acces.Statut, acces.Message);
        }

        if (fin <= debut || fin - debut > PeriodeMax)
        {
            return ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>.Echec(
                StatutOperation.Invalide, "La période demandée est invalide.");
        }

        return await LireAsync(acces.Valeur!, debut, fin, EvenementsParPage, PagesMax, cancellationToken);
    }

    /// <summary>Les prochains rendez-vous à heure fixe (sans les événements sur la journée entière), comme sur l'accueil.</summary>
    public async Task<ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>> GetProchainsAsync(
        string userId, CancellationToken cancellationToken)
    {
        var acces = await AccesAsync(userId, cancellationToken);
        if (acces.Statut != StatutOperation.Succes)
        {
            return ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>.Echec(acces.Statut, acces.Message);
        }

        var resultat = await LireAsync(acces.Valeur!, horloge.GetUtcNow(), null, ProchainsCandidats, 1, cancellationToken);
        return resultat.Statut != StatutOperation.Succes
            ? resultat
            : ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>.Succes(
                resultat.Valeur!.Where(e => !e.JourneeEntiere).Take(NombreProchains).ToList());
    }

    /// <summary>
    /// Calendriers de l'utilisatrice lisibles (liste Google), le principal d'abord : de quoi choisir celui qui est lu. Limité à
    /// la première page de Google (250 calendriers).
    /// </summary>
    public async Task<ResultatOperation<IReadOnlyList<CalendrierViewModel>>> ListerCalendriersAsync(
        string userId, CancellationToken cancellationToken)
    {
        var jeton = await liaison.JetonAccesAsync(userId, cancellationToken);
        if (jeton.Statut != StatutOperation.Succes)
        {
            return ResultatOperation<IReadOnlyList<CalendrierViewModel>>.Echec(
                jeton.Statut, jeton.Statut == StatutOperation.Indisponible ? MessageIndisponible : null);
        }

        try
        {
            using var requete = new HttpRequestMessage(HttpMethod.Get,
                $"{UrlListeCalendriers}?minAccessRole=reader&maxResults=250&fields=items(id,summary,summaryOverride,primary)");
            requete.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jeton.Valeur);
            using var reponse = await httpClient.SendAsync(requete, cancellationToken);
            if (!reponse.IsSuccessStatusCode)
            {
                logger.LogWarning("Google Calendar list request failed with status {StatusCode}", (int)reponse.StatusCode);
                return ResultatOperation<IReadOnlyList<CalendrierViewModel>>.Echec(StatutOperation.Indisponible, MessageIndisponible);
            }

            var contenu = await reponse.Content.ReadFromJsonAsync<ReponseListeGoogle>(cancellationToken);
            var calendriers = (contenu?.Items ?? [])
                .Where(c => !string.IsNullOrEmpty(c.Id))
                .Select(c => new CalendrierViewModel(
                    c.Id!, c.SummaryOverride ?? c.Summary ?? c.Id!, c.Primary == true))
                .OrderByDescending(c => c.Principal)
                .ThenBy(c => c.Nom, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            return ResultatOperation<IReadOnlyList<CalendrierViewModel>>.Succes(calendriers);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            logger.LogWarning("Google Calendar list request failed ({ExceptionType})", ex.GetType().Name);
            return ResultatOperation<IReadOnlyList<CalendrierViewModel>>.Echec(StatutOperation.Indisponible, MessageIndisponible);
        }
    }

    /// <summary>
    /// Enregistre le calendrier choisi. L'identifiant reçu n'est jamais pris tel quel : il doit figurer dans la liste de
    /// calendriers que Google renvoie pour cette utilisatrice.
    /// </summary>
    public async Task<ResultatOperation> ChoisirCalendrierAsync(string userId, string calendrierId, CancellationToken cancellationToken)
    {
        var calendriers = await ListerCalendriersAsync(userId, cancellationToken);
        if (calendriers.Statut != StatutOperation.Succes)
        {
            return ResultatOperation.Echec(calendriers.Statut, calendriers.Message);
        }

        if (calendriers.Valeur!.All(c => c.Id != calendrierId))
        {
            return ResultatOperation.Echec(StatutOperation.Invalide, "Ce calendrier n'est pas disponible.");
        }

        return await liaison.EnregistrerCalendrierAsync(userId, calendrierId, cancellationToken);
    }

    /// <summary>
    /// Le dernier rendez-vous à heure fixe commencé avant <paramref name="avant"/> (un an au plus en arrière), pour régler la période
    /// de « Préparer ce rendez-vous ». Liste de zéro ou un élément : « aucun » n'est pas une erreur.
    /// </summary>
    public async Task<ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>> GetPrecedentAsync(
        string userId, DateTimeOffset avant, CancellationToken cancellationToken)
    {
        var acces = await AccesAsync(userId, cancellationToken);
        if (acces.Statut != StatutOperation.Succes)
        {
            return ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>.Echec(acces.Statut, acces.Message);
        }

        if (avant.Year < 2000)
        {
            return ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>.Echec(
                StatutOperation.Invalide, "La date demandée est invalide.");
        }

        var resultat = await LireAsync(
            acces.Valeur!, avant - RemonteeRendezVousPrecedent, avant, EvenementsParPage, PagesMax, cancellationToken);
        if (resultat.Statut != StatutOperation.Succes)
        {
            return resultat;
        }

        var dernier = resultat.Valeur!
            .Where(e => !e.JourneeEntiere && DateTimeOffset.TryParse(e.Debut, CultureInfo.InvariantCulture, out var debut) && debut < avant)
            .OrderBy(e => DateTimeOffset.Parse(e.Debut, CultureInfo.InvariantCulture))
            .LastOrDefault();
        return ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>.Succes(dernier is null ? [] : [dernier]);
    }

    /// <summary>La liaison OAuth prime ; sans liaison, l'entrée de configuration (clé API) ; sinon Introuvable.</summary>
    private async Task<ResultatOperation<AccesAgenda>> AccesAsync(string userId, CancellationToken cancellationToken)
    {
        var jeton = await liaison.JetonAccesAsync(userId, cancellationToken);
        if (jeton.Statut == StatutOperation.Succes)
        {
            // Seul le calendrier choisi est lu : sans choix, aucun appel à Google pour des événements.
            var choisi = await liaison.CalendrierChoisiAsync(userId, cancellationToken);
            return choisi is null
                ? ResultatOperation<AccesAgenda>.Echec(StatutOperation.Introuvable)
                : ResultatOperation<AccesAgenda>.Succes(new AccesAgenda(
                    choisi,
                    r => r.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jeton.Valeur)));
        }

        if (jeton.Statut == StatutOperation.Indisponible)
        {
            return ResultatOperation<AccesAgenda>.Echec(StatutOperation.Indisponible, MessageIndisponible);
        }

        var calendrier = options.Value.CalendrierDe(userId);
        return calendrier is null
            ? ResultatOperation<AccesAgenda>.Echec(StatutOperation.Introuvable)
            : ResultatOperation<AccesAgenda>.Succes(
                new AccesAgenda(calendrier, r => r.Headers.Add(EnteteCleApi, options.Value.CleApi)));
    }

    private sealed record AccesAgenda(string Calendrier, Action<HttpRequestMessage> Authentifier);

    private async Task<ResultatOperation<IReadOnlyList<EvenementAgendaViewModel>>> LireAsync(
        AccesAgenda acces, DateTimeOffset debut, DateTimeOffset? fin, int parPage, int pagesMax,
        CancellationToken cancellationToken)
    {
        var evenements = new List<EvenementAgendaViewModel>();
        string? pageSuivante = null;
        try
        {
            for (var page = 0; page < pagesMax; page++)
            {
                using var requete = new HttpRequestMessage(HttpMethod.Get, Url(acces.Calendrier, debut, fin, parPage, pageSuivante));
                // Jeton ou clé en en-tête plutôt qu'en query string : HttpClient journalise les URL (niveau Information).
                acces.Authentifier(requete);
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

    private sealed record ReponseListeGoogle([property: JsonPropertyName("items")] List<CalendrierGoogle>? Items);

    private sealed record CalendrierGoogle(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("summary")] string? Summary,
        [property: JsonPropertyName("summaryOverride")] string? SummaryOverride,
        [property: JsonPropertyName("primary")] bool? Primary);

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
