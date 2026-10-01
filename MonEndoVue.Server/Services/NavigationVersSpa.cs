namespace MonEndoVue.Server.Services;

/// <summary>
/// Une page de l'application (<c>/cycle</c>, <c>/activite</c>…) et un endpoint de l'API (<c>GET /Cycle</c>) peuvent avoir le
/// même chemin, le routage ne tenant pas compte de la casse : au rafraîchissement, le navigateur recevait alors la réponse
/// JSON de l'API au lieu de la page. Toute navigation de navigateur (<c>Accept: text/html</c>) est donc servie par la page
/// d'entrée de l'application ; les appels de l'API (axios, fetch, images) n'en envoient jamais.
/// </summary>
public static class NavigationVersSpa
{
    /// <summary>
    /// Chemins servis par le serveur même pour une navigation : sonde, documentation, et retour de Google lors de la liaison de
    /// l'agenda (redirection du navigateur vers l'API, qui répond elle-même par une redirection vers Paramètres).
    /// </summary>
    private static readonly string[] CheminsDuServeur = ["/health", "/swagger", "/Agenda/liaison/callback"];

    public static bool EstNavigationDePage(HttpRequest requete) =>
        (HttpMethods.IsGet(requete.Method) || HttpMethods.IsHead(requete.Method))
        && requete.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase)
        && !Path.HasExtension(requete.Path.Value)
        && !CheminsDuServeur.Any(c => requete.Path.StartsWithSegments(c, StringComparison.OrdinalIgnoreCase));

    /// <summary>Réécrit le chemin vers la page d'entrée, avant le routage, si la requête est une navigation de page.</summary>
    public static void Appliquer(HttpRequest requete)
    {
        if (EstNavigationDePage(requete)) requete.Path = "/index.html";
    }
}
