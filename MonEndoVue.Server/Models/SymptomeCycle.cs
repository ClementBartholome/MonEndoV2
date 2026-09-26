namespace MonEndoVue.Server.Models;

public class SymptomeCycle
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    public string TypeSymptome { get; set; }
    public DateTime Date { get; set; }
    public int Intensite { get; set; }
    public string? Commentaire { get; set; }
    public string? PhotoUrl { get; set; }
}