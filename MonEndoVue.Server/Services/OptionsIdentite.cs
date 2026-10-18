using Microsoft.AspNetCore.Identity;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services.Comptes;

namespace MonEndoVue.Server.Services;

/// <summary>Réglages d'ASP.NET Core Identity, partagés par l'application et les tests.</summary>
public static class OptionsIdentite
{
    public const int EchecsAvantVerrouillage = 5;
    public static readonly TimeSpan DureeVerrouillage = TimeSpan.FromMinutes(15);

    /// <summary>Fournisseurs de jetons (confirmation d'adresse 24 h, réinitialisation 1 h), partagés par l'application et les tests.</summary>
    public static IdentityBuilder AjouterFournisseursDeJetons(this IdentityBuilder builder)
    {
        builder.Services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(24));
        builder.Services.AddOptions<OptionsJetonReinitialisation>();
        return builder.AddTokenProvider<FournisseurJetonReinitialisation>(FournisseurJetonReinitialisation.Nom);
    }

    public static void Appliquer(IdentityOptions options)
    {
        options.SignIn.RequireConfirmedAccount = true;
        // Jetons de réinitialisation : 1 h (fournisseur dédié) ; confirmation d'adresse : 24 h (fournisseur par défaut).
        options.Tokens.PasswordResetTokenProvider = FournisseurJetonReinitialisation.Nom;
        options.User.RequireUniqueEmail = true;
        // En plus de la limite de débit par adresse IP : un compte est verrouillé après plusieurs mots de passe erronés.
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = EchecsAvantVerrouillage;
        options.Lockout.DefaultLockoutTimeSpan = DureeVerrouillage;
    }
}
