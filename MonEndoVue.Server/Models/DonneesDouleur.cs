namespace MonEndoVue.Server.Models;

public class DonneesDouleur
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    public int Intensite { get; set; }
    public string TypeDouleur { get; set; }
    public DateTime Date { get; set; }
    public string? Commentaire { get; set; }
}