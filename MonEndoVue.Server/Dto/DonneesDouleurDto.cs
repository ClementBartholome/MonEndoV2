using System.ComponentModel.DataAnnotations;

namespace MonEndoVue.Server.Dto;

/// <summary>
/// Champs modifiables d'une douleur. L'identifiant vient de la route et le carnet de l'entrée en base.
/// </summary>
public class DonneesDouleurDto
{
    [Required]
    public string TypeDouleur { get; set; } = string.Empty;

    [Range(0, 10)]
    public int Intensite { get; set; }

    public DateTime Date { get; set; }

    public string? Commentaire { get; set; }
}
