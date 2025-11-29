namespace MonEndoVue.Server.Dto;

public class DonneesTraitementNonMedicamenteuxDto
{
    public int CarnetSanteId { get; set; }
    public int MedicamentId { get; set; }
    public int? Duree { get; set; }
    public DateTime Date { get; set; }
    public string? Commentaire { get; set; }
}
