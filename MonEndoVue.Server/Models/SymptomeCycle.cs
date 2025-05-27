using TypeGen.Core.TypeAnnotations;

namespace MonEndoVue.Server.Models;

[ExportTsInterface]
public class SymptomeCycle
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    public string TypeSymptome { get; set; }
    public DateTime Date { get; set; }
    public int Intensite { get; set; }
    [TsOptional]
    public string? Commentaire { get; set; }
}