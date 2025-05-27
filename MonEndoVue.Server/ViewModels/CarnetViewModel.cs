using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.ViewModels;

public class CarnetViewModel
{
    public string? UserName { get; set; }
    public int CarnetSanteId { get; set; }
    public List<DonneesDouleur>? DonneesDouleur { get; set; }
    public List<DonneesActivitePhysique>? DonneesActivitePhysique { get; set; }
    public IEnumerable<DonneesMedicamentViewModel>? DonneesMedicament { get; set; }
    public List<DonneesTransit>? DonneesTransit { get; set; }
    public List<Medicament>? Medicaments { get; set; }
    public List<JourRegle>? JourRegles { get; set; }
    public List<BilanQuotidien>? BilansQuotidiens { get; set; }
}