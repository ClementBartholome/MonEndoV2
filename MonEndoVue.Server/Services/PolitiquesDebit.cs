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

    public static void AppliquerParDefaut(EndpointBuilder endpoint, string politique)
    {
        if (endpoint.Metadata.Any(m => m is EnableRateLimitingAttribute or DisableRateLimitingAttribute))
        {
            return;
        }

        endpoint.Metadata.Add(new EnableRateLimitingAttribute(politique));
    }
}
