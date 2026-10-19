using System.Text;

namespace MonEndoVue.Server.Services;

/// <summary>
/// Clé de signature des jetons, lue à un seul endroit (<c>Authentication:Schemes:Bearer:Secret</c>) pour émettre
/// et valider : deux réglages pouvaient diverger et invalider toutes les sessions.
/// </summary>
public static class CleSignatureJwt
{
    public const string Reglage = "Authentication:Schemes:Bearer:Secret";

    /// <summary>En dessous, la clé est jugée trop courte pour HMAC-SHA256 (avertissement au démarrage).</summary>
    public const int LongueurRecommandee = 32;

    public static byte[] Lire(IConfiguration configuration)
    {
        var cle = configuration[Reglage];
        if (string.IsNullOrWhiteSpace(cle))
        {
            throw new InvalidOperationException($"La clé de signature des jetons ({Reglage}) n'est pas configurée.");
        }

        return Encoding.ASCII.GetBytes(cle);
    }
}
