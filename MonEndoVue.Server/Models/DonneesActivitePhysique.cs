using TypeGen.Core.TypeAnnotations;

namespace MonEndoVue.Server.Models;

[ExportTsInterface]
public class DonneesActivitePhysique
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    public string TypeActivite { get; set; }
    public DateTime Date { get; set; }
    public int Duree { get; set; }
    public int Intensite { get; set; }
    public int EffetDouleur { get; set; }
    [TsOptional]
    public string? Commentaire { get; set; }
}