using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.ViewModels;

public class MedicamentViewModel
{
    public int Id { get; set; }
    public string Nom { get; set; }
    public TypeTraitement Type { get; set; }
    public string? Posologie { get; set; }
    public bool TraitementEnCours { get; set; }
    public DateTime DateDebutTraitement { get; set; }
    public DateTime? DateFinTraitement { get; set; }
}