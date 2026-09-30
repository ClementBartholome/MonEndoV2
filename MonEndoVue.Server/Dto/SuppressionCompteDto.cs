using System.ComponentModel.DataAnnotations;

namespace MonEndoVue.Server.Dto;

/// <summary>Confirmation de la suppression du compte par le mot de passe, dans le corps de la requête.</summary>
public class SuppressionCompteDto
{
    [Required]
    public string Password { get; set; } = string.Empty;
}
