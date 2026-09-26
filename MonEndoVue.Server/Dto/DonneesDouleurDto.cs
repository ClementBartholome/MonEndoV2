using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MonEndoVue.Server.Dto;

/// <summary>
/// Champs modifiables d'une douleur. L'identifiant vient de la route et le carnet de l'entrée en base.
/// </summary>
public class DonneesDouleurDto
{
    [Required]
    public string TypeDouleur { get; set; } = string.Empty;

    [JsonRequired]
    [Range(0, 10)]
    public int Intensite { get; set; }

    [JsonRequired]
    public DateTime Date { get; set; }

    public string? Commentaire { get; set; }
}
