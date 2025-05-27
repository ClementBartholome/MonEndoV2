using MonEndoVue.Server.Data;

namespace MonEndoVue.Server.Models;

public class CarnetSante
{
    public int Id { get; set; }
    public string UserId { get; set; }
    
    public ApplicationUser? User { get; set; }
    public List<DonneesDouleur> DonneesDouleurs { get; set; }
    public List<DonneesActivitePhysique> DonneesActivitePhysique { get; set; }
    public List<DonneesMedicament> DonneesMedicaments { get; set; }
    public List<Medicament> Medicaments { get; set; }
    public List<DonneesTransit> DonneesTransit { get; set; }
    public List<JourRegle> JourRegles { get; set; }
    public List<BilanQuotidien> BilansQuotidiens { get; set; }
    public List<SymptomeCycle> SymptomesCycles { get; set; }
}