using System.ComponentModel.DataAnnotations;

namespace MonEndoVue.Server.Dto;

/// <summary>Une adresse e-mail seule (renvoi du lien de confirmation, mot de passe oublié), dans le corps de la requête.</summary>
public class AdresseDto
{
    [Required]
    public string Email { get; set; } = string.Empty;
}

/// <summary>Confirmation de l'adresse : le jeton est lu dans le fragment du lien par la page, puis posté ici (jamais dans l'URL).</summary>
public class ConfirmationAdresseDto
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Jeton { get; set; } = string.Empty;
}

/// <summary>Nouveau mot de passe choisi depuis le lien de réinitialisation.</summary>
public class ReinitialisationMotDePasseDto
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Jeton { get; set; } = string.Empty;

    [Required]
    public string NouveauMotDePasse { get; set; } = string.Empty;
}
