using TypeGen.Core.TypeAnnotations;

namespace MonEndoVue.Server.Models;

[ExportTsInterface]
public class Medicament
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    public string Nom { get; set; }
    public string Posologie { get; set; }
    public bool TraitementEnCours { get; set; }
    [TsOptional]
    public DateTime DateDebutTraitement { get; set; }
    [TsOptional]
    public DateTime? DateFinTraitement { get; set; }
}