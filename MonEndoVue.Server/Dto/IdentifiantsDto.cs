using System.ComponentModel.DataAnnotations;

namespace MonEndoVue.Server.Dto;

/// <summary>Identifiants de connexion ou d'inscription, transmis dans le corps de la requête (jamais dans l'URL).</summary>
public class IdentifiantsDto
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
