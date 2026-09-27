using Microsoft.AspNetCore.Identity;

namespace MonEndoVue.Server.Services;

/// <summary>Réglages d'ASP.NET Core Identity, partagés par l'application et les tests.</summary>
public static class OptionsIdentite
{
    public const int EchecsAvantVerrouillage = 5;
    public static readonly TimeSpan DureeVerrouillage = TimeSpan.FromMinutes(15);

    public static void Appliquer(IdentityOptions options)
    {
        options.SignIn.RequireConfirmedAccount = true;
        // En plus de la limite de débit par adresse IP : un compte est verrouillé après plusieurs mots de passe erronés.
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = EchecsAvantVerrouillage;
        options.Lockout.DefaultLockoutTimeSpan = DureeVerrouillage;
    }
}
