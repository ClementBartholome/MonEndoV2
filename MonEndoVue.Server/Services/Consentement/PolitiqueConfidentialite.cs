using System.Security.Claims;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services.Consentement;

/// <summary>
/// Consentement explicite au traitement des données de santé (RGPD, art. 9.2.a), donné pour une version de la
/// politique de confidentialité. La version acceptée est portée par le jeton d'accès : une utilisatrice sans
/// consentement à jour ne peut appeler que les endpoints marqués <see cref="SansConsentementAttribute"/>.
/// </summary>
public static class PolitiqueConfidentialite
{
    /// <summary>
    /// Version en vigueur (date de la politique, <c>features/legal/config/editeur.ts</c> côté client).
    /// La changer lors d'une évolution importante de la politique redemande le consentement à toutes les utilisatrices.
    /// </summary>
    public const string Version = "2026-09-27";

    /// <summary>Claim du jeton d'accès portant la version acceptée.</summary>
    public const string TypeClaim = "politique";

    /// <summary>Code renvoyé (403) quand le consentement manque, lu par le client pour afficher la page d'accord.</summary>
    public const string CodeConsentementRequis = "consentement-requis";

    public static bool EstAJour(ApplicationUser user) => user.VersionPolitiqueAcceptee == Version;

    public static bool EstAJour(ClaimsPrincipal principal) => principal.FindFirstValue(TypeClaim) == Version;

    public static void Enregistrer(ApplicationUser user, DateTimeOffset maintenant)
    {
        user.ConsentementDonneesSanteLe = maintenant.UtcDateTime;
        user.VersionPolitiqueAcceptee = Version;
    }
}
