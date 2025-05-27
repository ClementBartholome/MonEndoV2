namespace MonEndoVue.Server.ViewModels;

public class DonneesMedicamentViewModel
{
    public int Id { get; set; }
    public string NomMedicament { get; set; } 
    public int NombreComprimes { get; set; }
    public DateTime Date { get; set; }
    public string? Commentaire { get; set; }
}