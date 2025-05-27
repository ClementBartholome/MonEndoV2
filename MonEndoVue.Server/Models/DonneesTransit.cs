using TypeGen.Core.TypeAnnotations;

namespace MonEndoVue.Server.Models;

[ExportTsInterface]
public class DonneesTransit
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    public DateTime Date { get; set; }
    public string TypeEvenement { get; set; } // Constipation, Diarrhée, etc
    public string Intensite { get; set; } // Légère, Modérée, Sévère
    public bool Saignement { get; set; }
    public bool Douleur { get; set; }
    [TsOptional]
    public string? Commentaires { get; set; } 
}