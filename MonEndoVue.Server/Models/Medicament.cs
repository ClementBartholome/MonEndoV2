namespace MonEndoVue.Server.Models;

public class Medicament
{
    public int Id { get; set; }
    public CarnetSante? CarnetSante { get; set; }
    public int CarnetSanteId { get; set; }
    public string Nom { get; set; }
    public TypeTraitement Type { get; set; }
    public string? Posologie { get; set; }
    public bool TraitementEnCours { get; set; }
    public DateTime DateDebutTraitement { get; set; }
    public DateTime? DateFinTraitement { get; set; }
}