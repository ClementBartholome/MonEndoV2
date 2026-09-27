namespace MonEndoVue.Server.Services;

/// <summary>En-têtes de sécurité HTTP posés sur toutes les réponses (pages et API).</summary>
public static class EntetesSecurite
{
    /// <summary>
    /// Politique de contenu appliquée (bloquante) depuis la 1.2.1, après une période en « Report-Only » sans violation
    /// relevée en production. Sources tierces : traduction DataTables, photos sur Azure Blob Storage.
    /// </summary>
    public static readonly string ContentSecurityPolicy = string.Join("; ",
        "default-src 'self'",
        "script-src 'self'",
        // Styles posés par les composants (radix-vue, graphiques, DataTables) : attributs style et balises <style>.
        "style-src 'self' 'unsafe-inline'",
        // data: : police d'icônes intégrée en base64 par une feuille de style d'une dépendance.
        "font-src 'self' data:",
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
        entetes.XFrameOptions = "DENY";
        entetes.XContentTypeOptions = "nosniff";
        entetes["Referrer-Policy"] = "strict-origin-when-cross-origin";
        entetes["Permissions-Policy"] = "camera=(self), microphone=(), geolocation=()";
        entetes.ContentSecurityPolicy = ContentSecurityPolicy;
    }
}
