using Microsoft.AspNetCore.Identity;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services;

public enum IssueVerificationMotDePasse
{
    Correct,
    Incorrect,
    Verrouille,
}

/// <summary>
/// Vérification d'un mot de passe pour une action sensible d'une session déjà ouverte (changement de mot de passe,
/// suppression du compte). <c>CheckPasswordAsync</c> ne compte pas les échecs : une session volée pourrait essayer des mots
/// de passe sans fin. Ici les échecs comptent comme à la connexion (même seuil, même verrouillage).
/// </summary>
public static class VerificationMotDePasse
{
    public const string MessageTropDeTentatives = "Trop d'essais. Réessaie dans quelques minutes.";

    public static async Task<IssueVerificationMotDePasse> VerifierAsync(
        this UserManager<ApplicationUser> userManager, ApplicationUser user, string motDePasse)
    {
        if (await userManager.IsLockedOutAsync(user)) return IssueVerificationMotDePasse.Verrouille;

        if (await userManager.CheckPasswordAsync(user, motDePasse))
        {
            await userManager.ResetAccessFailedCountAsync(user);
            return IssueVerificationMotDePasse.Correct;
        }

        await userManager.AccessFailedAsync(user);
        return await userManager.IsLockedOutAsync(user)
            ? IssueVerificationMotDePasse.Verrouille
            : IssueVerificationMotDePasse.Incorrect;
    }
}
