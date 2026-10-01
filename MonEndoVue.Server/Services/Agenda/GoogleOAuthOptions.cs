namespace MonEndoVue.Server.Services.Agenda;

/// <summary>
/// Client OAuth Google pour la liaison de l'agenda (section de configuration « GoogleOAuth »). Sans identifiant, secret
/// et adresse de retour, la liaison est désactivée : le reste de l'application n'est pas affecté.
/// </summary>
public class GoogleOAuthOptions
{
    public const string Section = "GoogleOAuth";

    public string? ClientId { get; set; }

    /// <summary>Secret du client, jamais transmis au navigateur.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Adresse de retour enregistrée chez Google : https://&lt;domaine&gt;/Agenda/liaison/callback.</summary>
    public string? RedirectUri { get; set; }

    /// <summary>
    /// Page Paramètres où ramener l'utilisatrice après Google, si l'application n'est pas servie par l'API elle-même (dev : front
    /// Vite sur un autre port). Absente : chemin relatif, c'est-à-dire la page de l'API (production). Lue en configuration seulement.
    /// </summary>
    public string? RetourApplication { get; set; }

    public bool EstConfiguree =>
        !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret)
        && Uri.TryCreate(RedirectUri, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps;
}
