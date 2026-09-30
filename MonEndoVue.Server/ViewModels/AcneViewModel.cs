namespace MonEndoVue.Server.ViewModels;

/// <summary>Onglet Acné : épisodes (du plus récent au plus ancien, le premier éventuellement en cours) et suivis photo.</summary>
public class AcneViewModel
{
    public List<EpisodeAcneViewModel> Episodes { get; set; } = [];

    /// <summary>Points de suivi avec photo de la fenêtre demandée, du plus récent au plus ancien.</summary>
    public List<SuiviAcneViewModel> Suivis { get; set; } = [];

    /// <summary>Photos plus anciennes que la fenêtre, à demander avec <c>mois=</c>.</summary>
    public int SuivisPlusAnciens { get; set; }
}

public class EpisodeAcneViewModel
{
    public int Id { get; set; }

    /// <summary>« yyyy-MM-dd »</summary>
    public string Debut { get; set; } = string.Empty;

    /// <summary>« yyyy-MM-dd », null si l'épisode est en cours.</summary>
    public string? Fin { get; set; }

    /// <summary>Nombre de jours, jour demandé compris pour un épisode en cours.</summary>
    public int Jours { get; set; }
}

public class SuiviAcneViewModel
{
    public int Id { get; set; }

    /// <summary>Date locale sans fuseau (« yyyy-MM-ddTHH:mm:ss »).</summary>
    public string Date { get; set; } = string.Empty;

    public int Intensite { get; set; }

    public string? Commentaire { get; set; }

    public string PhotoUrl { get; set; } = string.Empty;
}
