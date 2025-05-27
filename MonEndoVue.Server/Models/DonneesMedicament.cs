using TypeGen.Core.TypeAnnotations;

namespace MonEndoVue.Server.Models;

[ExportTsInterface]
public class DonneesMedicament
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    public int MedicamentId { get; set; }
    public int NombreComprimes { get; set; }
    public DateTime Date { get; set; }
    [TsOptional]
    public string? Commentaire { get; set; }
    [TsOptional]
    public Medicament? Medicament { get; set; }
}