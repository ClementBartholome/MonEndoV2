using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.ViewModels;

public class CarnetHomepageViewModel
{
    public string? UserName { get; set; }
    public int CarnetSanteId { get; set; }
    public DonneesDouleur? DonneesDouleur { get; set; }
    public DonneesActivitePhysique? DonneesActivitePhysique { get; set; }
    public DonneesMedicament? DonneesMedicament { get; set; }
    public DonneesTransit? DonneesTransit { get; set; }
    public string? NomMedicament { get; set; }
    public JourRegle? JourRegle { get; set; }
}