namespace MonEndoVue.Server.Models;

public class DonneesTraitementNonMedicamenteux
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    public int MedicamentId { get; set; }
    public Medicament? Medicament { get; set; }
    public int? Duree { get; set; }
    public DateTime Date { get; set; }
    public string? Commentaire { get; set; }
}
