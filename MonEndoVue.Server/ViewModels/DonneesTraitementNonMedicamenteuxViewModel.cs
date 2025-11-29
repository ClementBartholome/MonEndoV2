namespace MonEndoVue.Server.ViewModels;

public class DonneesTraitementNonMedicamenteuxViewModel
{
    public int Id { get; set; }
    public string NomTraitement { get; set; }
    public int? Duree { get; set; }
    public DateTime Date { get; set; }
    public string? Commentaire { get; set; }
}
