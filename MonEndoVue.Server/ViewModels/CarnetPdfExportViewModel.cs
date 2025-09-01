namespace MonEndoVue.Server.ViewModels;

public class CarnetPdfExportViewModel
{
    public string? UserName { get; set; }
    public int CarnetSanteId { get; set; }
    public List<CarnetPdfMedicamentViewModel> Medicaments { get; set; } = new();
    public List<DonneesMedicamentExportViewModel> DonneesMedicament { get; set; } = new();
    public List<JourRegleViewModel> JourRegles { get; set; } = new();
    public List<DonneesDouleurExportViewModel> DonneesDouleur { get; set; } = new();
    public List<DonneesActivitePhysiqueExportViewModel> DonneesActivitePhysique { get; set; } = new();
    public List<DonneesTransitExportViewModel> DonneesTransit { get; set; } = new();
    public List<BilanQuotidienExportViewModel> BilansQuotidiens { get; set; } = new();
}

public class CarnetPdfMedicamentViewModel
{
    public int Id { get; set; }
    public string Nom { get; set; }
}

public class DonneesMedicamentExportViewModel
{
    public int Id { get; set; }
    public int MedicamentId { get; set; }
    public string NomMedicament { get; set; }
    public DateTime Date { get; set; }
}

public class DonneesDouleurExportViewModel
{
    public string TypeDouleur { get; set; }
    public DateTime Date { get; set; }
    public int Intensite { get; set; }
}

public class DonneesActivitePhysiqueExportViewModel
{
    public string TypeActivite { get; set; }
    public DateTime Date { get; set; }
    public int Intensite { get; set; }
}

public class DonneesTransitExportViewModel
{
    public string TypeEvenement { get; set; }
    public DateTime Date { get; set; }
}

public class BilanQuotidienExportViewModel
{
    public DateTime Date { get; set; }
    public bool Lactose { get; set; }
    public bool Grignotage { get; set; }
    public bool Gluten { get; set; }
    public int Fatigue { get; set; }
    public int Pas { get; set; }
    public int DouleurMoyenne { get; set; }
    public double Hydratation { get; set; }
    public double StressMoyenne { get; set; }
}