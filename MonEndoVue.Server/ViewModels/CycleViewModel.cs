namespace MonEndoVue.Server.ViewModels;

/// <summary>Page Cycle, onglet Règles : mois affiché, cycle en cours et historique (<c>GET Cycle</c>).</summary>
public class CycleViewModel
{
    /// <summary>Jours de règles notés dans le mois demandé (« yyyy-MM-dd »).</summary>
    public List<string> JoursDeRegles { get; set; } = [];

    /// <summary>Cycle en cours le jour local de l'utilisatrice ; null sans règles notées depuis 60 jours.</summary>
    public CycleEnCoursViewModel? EnCours { get; set; }

    /// <summary>Derniers cycles terminés, du plus récent au plus ancien.</summary>
    public List<CycleTermineViewModel> Cycles { get; set; } = [];

    /// <summary>Durée moyenne des cycles listés ; null avant deux cycles.</summary>
    public int? DureeMoyenne { get; set; }
}

public class CycleEnCoursViewModel
{
    /// <summary>Premier jour des dernières règles (« yyyy-MM-dd »).</summary>
    public string Debut { get; set; } = string.Empty;

    public int JourDuCycle { get; set; }

    /// <summary>Jour de règles si le jour demandé est noté, sinon null.</summary>
    public int? JourDeRegles { get; set; }
}

public class CycleTermineViewModel
{
    /// <summary>« yyyy-MM-dd »</summary>
    public string Debut { get; set; } = string.Empty;

    public int JoursDeRegles { get; set; }

    public int Duree { get; set; }
}
