using TypeGen.Core.TypeAnnotations;

namespace MonEndoVue.Server.Models;

[ExportTsInterface]
public class DonneesDouleur
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    public int Intensite { get; set; }
    public string TypeDouleur { get; set; }
    public DateTime Date { get; set; }
    [TsOptional]
    public string? Commentaire { get; set; }
}