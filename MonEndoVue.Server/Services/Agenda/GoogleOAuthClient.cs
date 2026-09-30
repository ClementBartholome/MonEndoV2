using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace MonEndoVue.Server.Services.Agenda;

/// <summary>Jetons renvoyés par Google ; le jeton d'actualisation n'est présent qu'à l'échange du code.</summary>
public sealed record JetonsGoogle(string JetonAcces, string? JetonActualisation, TimeSpan Validite);

/// <summary>Échec d'un appel de jeton : <see cref="Revoque"/> quand Google indique que l'accord n'est plus valide.</summary>
public sealed record EchecJetonGoogle(bool Revoque);

/// <summary>Appels HTTP à Google OAuth (URL d'autorisation, échange du code, actualisation, révocation).</summary>
public class GoogleOAuthClient(HttpClient httpClient, IOptions<GoogleOAuthOptions> options, ILogger<GoogleOAuthClient> logger)
{
    public const string UrlAutorisation = "https://accounts.google.com/o/oauth2/v2/auth";
    public const string UrlJeton = "https://oauth2.googleapis.com/token";
    public const string UrlRevocation = "https://oauth2.googleapis.com/revoke";

    /// <summary>Lecture des événements uniquement : la portée la plus étroite qui suffit à lister ceux du calendrier principal.</summary>
    public const string Portee = "https://www.googleapis.com/auth/calendar.events.readonly";

    public string UrlAutorisationPour(string etat, string defiPkce)
    {
        var parametres = new Dictionary<string, string>
        {
            ["client_id"] = options.Value.ClientId!,
            ["redirect_uri"] = options.Value.RedirectUri!,
            ["response_type"] = "code",
            ["scope"] = Portee,
            ["state"] = etat,
            ["code_challenge"] = defiPkce,
            ["code_challenge_method"] = "S256",
            // Jeton d'actualisation à chaque liaison, y compris si un ancien accord existe déjà chez Google.
            ["access_type"] = "offline",
            ["prompt"] = "consent",
        };
        return $"{UrlAutorisation}?{string.Join('&', parametres.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"))}";
    }

    public Task<(JetonsGoogle? Jetons, EchecJetonGoogle? Echec)> EchangerCodeAsync(
        string code, string verificateurPkce, CancellationToken ct) =>
        DemanderJetonsAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["code_verifier"] = verificateurPkce,
            ["redirect_uri"] = options.Value.RedirectUri!,
        }, ct);

    public Task<(JetonsGoogle? Jetons, EchecJetonGoogle? Echec)> ActualiserAsync(string jetonActualisation, CancellationToken ct) =>
        DemanderJetonsAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = jetonActualisation,
        }, ct);

    /// <summary>Révoque l'accord chez Google. Un échec est journalisé sans jamais bloquer la déliaison locale.</summary>
    public async Task RevoquerAsync(string jeton, CancellationToken ct)
    {
        try
        {
            using var reponse = await httpClient.PostAsync(
                UrlRevocation, new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = jeton }), ct);
            if (!reponse.IsSuccessStatusCode)
            {
                logger.LogWarning("Google token revocation failed with status {StatusCode}", (int)reponse.StatusCode);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning("Google token revocation failed ({ExceptionType})", ex.GetType().Name);
        }
    }

    private async Task<(JetonsGoogle?, EchecJetonGoogle?)> DemanderJetonsAsync(Dictionary<string, string> champs, CancellationToken ct)
    {
        champs["client_id"] = options.Value.ClientId!;
        champs["client_secret"] = options.Value.ClientSecret!;
        try
        {
            using var reponse = await httpClient.PostAsync(UrlJeton, new FormUrlEncodedContent(champs), ct);
            if (!reponse.IsSuccessStatusCode)
            {
                // Ni code, ni jeton, ni secret dans les logs : le statut et le code d'erreur Google suffisent.
                var erreur = await LireErreurAsync(reponse, ct);
                logger.LogWarning("Google token request failed with status {StatusCode} ({GoogleError})", (int)reponse.StatusCode, erreur);
                return (null, new EchecJetonGoogle(erreur == "invalid_grant"));
            }

            var contenu = await reponse.Content.ReadFromJsonAsync<ReponseJeton>(ct);
            return string.IsNullOrEmpty(contenu?.AccessToken)
                ? (null, new EchecJetonGoogle(false))
                : (new JetonsGoogle(contenu.AccessToken, contenu.RefreshToken, TimeSpan.FromSeconds(contenu.ExpiresIn)), null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException && !ct.IsCancellationRequested)
        {
            logger.LogWarning("Google token request failed ({ExceptionType})", ex.GetType().Name);
            return (null, new EchecJetonGoogle(false));
        }
    }

    private static async Task<string?> LireErreurAsync(HttpResponseMessage reponse, CancellationToken ct)
    {
        try
        {
            return (await reponse.Content.ReadFromJsonAsync<ReponseErreur>(ct))?.Error;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private sealed record ReponseJeton(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private sealed record ReponseErreur([property: JsonPropertyName("error")] string? Error);
}
