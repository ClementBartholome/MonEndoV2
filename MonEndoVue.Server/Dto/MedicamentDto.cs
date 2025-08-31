namespace MonEndoVue.Server.Dto;

public class MedicamentDto
{
    public int CarnetSanteId { get; set; }
    public string Nom { get; set; }
    public string Posologie { get; set; }
    public bool TraitementEnCours { get; set; }
    public DateTime DateDebutTraitement { get; set; }
    public DateTime? DateFinTraitement { get; set; }
}