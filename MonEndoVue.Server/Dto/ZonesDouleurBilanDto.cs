using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MonEndoVue.Server.Dto;

/// <summary>Zones touchées dans le bilan d'un jour (« Où as-tu eu mal ? ») : elles remplacent les précédentes.</summary>
public class ZonesDouleurBilanDto
{
    [Required]
    public List<ZoneDouleurDto> Zones { get; set; } = [];
}

public class ZoneDouleurDto
{
    [Required]
    public string TypeDouleur { get; set; } = string.Empty;

    [JsonRequired]
    public int Intensite { get; set; }
}
