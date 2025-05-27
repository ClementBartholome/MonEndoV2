using TypeGen.Core.TypeAnnotations;

namespace MonEndoVue.Server.Models;

[ExportTsInterface]
public class JourRegle
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    // public int CycleId { get; set; }
    public DateTime Date { get; set; }
    // public float Temperature { get; set; }
    // public string FluxMenstruel { get; set; }    
}