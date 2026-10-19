namespace MonEndoVue.Server.Models;

public class JourRegle
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    // public int CycleId { get; set; }
    public DateTime Date { get; set; }
    // public float Temperature { get; set; }
    /// <summary>Flux du jour ; null = non précisé (jamais compté comme un niveau).</summary>
    public FluxRegles? Flux { get; set; }

    /// <summary>Caillots ce jour-là ; null = non précisé (ni oui ni non).</summary>
    public bool? Caillots { get; set; }
}