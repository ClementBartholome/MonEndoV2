using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace MonEndoVue.Server.Services;

/// <summary>
/// Politiques de limitation de débit. <c>RequireRateLimiting</c> appliqué à tous les contrôleurs écrase les attributs
/// <c>[EnableRateLimiting]</c> des actions (la convention est ajoutée après) : la politique par défaut n'est donc posée
/// que sur les endpoints qui n'en déclarent pas.
/// </summary>
public static class PolitiquesDebit
{
    public const string Api = "api";
    public const string Auth = "auth";

    /// <summary>Fenêtre fixe d'une minute par utilisatrice connectée, ou par adresse pour un appel anonyme.</summary>
    public static RateLimitPartition<string> Partition(HttpContext contexte, int limite)
    {
        var cle = contexte.User.FindFirstValue(ClaimTypes.NameIdentifier) is { } id
            ? $"utilisatrice:{id}"
            : $"adresse:{contexte.Connection.RemoteIpAddress}";
        return RateLimitPartition.GetFixedWindowLimiter(cle, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limite,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    }

    public static void AppliquerParDefaut(EndpointBuilder endpoint, string politique)
    {
        if (endpoint.Metadata.Any(m => m is EnableRateLimitingAttribute or DisableRateLimitingAttribute))
        {
            return;
        }

        endpoint.Metadata.Add(new EnableRateLimitingAttribute(politique));
    }
}
