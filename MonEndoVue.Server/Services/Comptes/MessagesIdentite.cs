using Microsoft.AspNetCore.Identity;

namespace MonEndoVue.Server.Services.Comptes;

/// <summary>Messages d'Identity traduits pour l'utilisatrice (inscription, changement et réinitialisation du mot de passe).</summary>
public static class MessagesIdentite
{
    /// <summary>Adresse déjà inscrite : jamais renvoyée telle quelle (elle révélerait qu'un compte existe).</summary>
    public static readonly string[] CodesAdresseDejaPrise = ["DuplicateUserName", "DuplicateEmail"];

    public static string Traduire(IdentityError error) => error.Code switch
    {
        "PasswordTooShort" => "Le mot de passe doit contenir au moins huit caractères.",
        "PasswordRequiresNonAlphanumeric" => "Le mot de passe doit contenir au moins un caractère spécial.",
        "PasswordRequiresDigit" => "Le mot de passe doit contenir au moins un chiffre.",
        "PasswordRequiresLower" => "Le mot de passe doit contenir au moins une lettre minuscule.",
        "PasswordRequiresUpper" => "Le mot de passe doit contenir au moins une lettre majuscule.",
        "DuplicateUserName" => "Ce nom d'utilisateur est déjà pris.",
        "InvalidEmail" => "L'adresse e-mail est invalide.",
        "InvalidUserName" => "L'adresse e-mail est invalide.",
        "DuplicateEmail" => "Cette adresse e-mail est déjà utilisée.",
        "PasswordMismatch" => "Le mot de passe actuel est incorrect.",
        _ => error.Description,
    };

    public static IReadOnlyList<string> Traduire(IEnumerable<IdentityError> errors) => errors.Select(Traduire).ToList();
}
