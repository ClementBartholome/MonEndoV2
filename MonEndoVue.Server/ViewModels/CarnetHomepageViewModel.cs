namespace MonEndoVue.Server.ViewModels;

public class CarnetHomepageViewModel
{
    public string? UserName { get; set; }
    public int CarnetSanteId { get; set; }
    public DonneesDouleurViewModel? DonneesDouleur { get; set; }
    public DonneesActivitePhysiqueViewModel? DonneesActivitePhysique { get; set; }
    public DonneesMedicamentHomepageViewModel? DonneesMedicament { get; set; }
    public DonneesTransitViewModel? DonneesTransit { get; set; }
    public JourRegleViewModel? JourRegle { get; set; }
}

public class DonneesDouleurViewModel
{
    public string TypeDouleur { get; set; }
    public DateTime Date { get; set; }
}

public class DonneesActivitePhysiqueViewModel
{
    public string TypeActivite { get; set; }
    public DateTime Date { get; set; }
}

public class DonneesMedicamentHomepageViewModel
{
    public int Id { get; set; }
    public string? NomMedicament { get; set; }
    public DateTime Date { get; set; }
}

public class DonneesTransitViewModel
{
    public string TypeEvenement { get; set; }
    public DateTime Date { get; set; }
}

public class JourRegleViewModel
{
    public DateTime Date { get; set; }
}