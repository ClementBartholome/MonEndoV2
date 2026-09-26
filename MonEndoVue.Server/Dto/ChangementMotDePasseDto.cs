using System.ComponentModel.DataAnnotations;

namespace MonEndoVue.Server.Dto;

/// <summary>Changement de mot de passe, transmis dans le corps de la requête (jamais dans l'URL).</summary>
public class ChangementMotDePasseDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    public string NewPassword { get; set; } = string.Empty;
}
