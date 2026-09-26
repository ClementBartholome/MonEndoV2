namespace MonEndoVue.Server.Services.WebPush;

/// <summary>Clés VAPID (section de configuration « WebPush »). Absentes ou invalides, l'envoi de notifications est désactivé.</summary>
public class WebPushOptions
{
    public const string Section = "WebPush";
    private const int LongueurClePublique = 87; // point P-256 non compressé (65 octets) en base64url
    private const int LongueurClePrivee = 43;   // scalaire P-256 (32 octets) en base64url

    /// <summary>Contact de l'émetteur, exigé par les services push (ex. mailto:contact@exemple.fr).</summary>
    public string? Subject { get; set; }
    public string? PublicKey { get; set; }
    public string? PrivateKey { get; set; }

    /// <summary>Ce qui empêche l'envoi, sans jamais citer de valeur secrète ; null si la configuration est valide.</summary>
    public string? Erreur()
    {
        if (string.IsNullOrWhiteSpace(Subject) && string.IsNullOrWhiteSpace(PublicKey) && string.IsNullOrWhiteSpace(PrivateKey))
        {
            return "section WebPush absente ou vide à la racine de la configuration";
        }

        if (!(Subject?.StartsWith("mailto:", StringComparison.Ordinal) ?? false) &&
            !(Subject?.StartsWith("https:", StringComparison.Ordinal) ?? false))
        {
            return "WebPush:Subject doit commencer par mailto: ou https:";
        }

        if (PublicKey?.Length != LongueurClePublique)
        {
            return $"WebPush:PublicKey doit faire {LongueurClePublique} caractères (actuellement {PublicKey?.Length ?? 0})";
        }

        if (PrivateKey?.Length != LongueurClePrivee)
        {
            return $"WebPush:PrivateKey doit faire {LongueurClePrivee} caractères (actuellement {PrivateKey?.Length ?? 0})";
        }

        return null;
    }

    public bool EstConfiguree => Erreur() is null;
}
