namespace MonEndoVue.Server.Services;

/// <summary>En-têtes de sécurité HTTP posés sur toutes les réponses (pages et API).</summary>
public static class EntetesSecurite
{
    /// <summary>
    /// Politique de contenu, en mode « Report-Only » : le navigateur signale les violations dans la console sans rien
    /// bloquer. À passer en <c>Content-Security-Policy</c> (blocage) une fois la politique vérifiée en production.
    /// Sources tierces : polices Google, traduction DataTables, photos sur Azure Blob Storage.
    /// </summary>
    public const string NomEnteteCsp = "Content-Security-Policy-Report-Only";

    public static readonly string ContentSecurityPolicy = string.Join("; ",
        "default-src 'self'",
        "script-src 'self'",
        // Styles posés par les composants (radix-vue, graphiques, DataTables) : attributs style et balises <style>.
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
        // data: : police d'icônes intégrée en base64 par une feuille de style d'une dépendance.
        "font-src 'self' data: https://fonts.gstatic.com",
        // data: et blob: : aperçu d'une photo avant envoi et export PDF.
        "img-src 'self' data: blob: https://*.blob.core.windows.net",
        "connect-src 'self' https://cdn.datatables.net",
        "worker-src 'self'",
        "manifest-src 'self'",
        "object-src 'none'",
        "base-uri 'self'",
        "form-action 'self'",
        "frame-ancestors 'none'");

    public static void Appliquer(IHeaderDictionary entetes)
    {
        entetes["X-Frame-Options"] = "DENY";
        entetes["X-Content-Type-Options"] = "nosniff";
        entetes["Referrer-Policy"] = "strict-origin-when-cross-origin";
        entetes["Permissions-Policy"] = "camera=(self), microphone=(), geolocation=()";
        entetes[NomEnteteCsp] = ContentSecurityPolicy;
    }
}
