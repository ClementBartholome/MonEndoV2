namespace MonEndoVue.Server.ViewModels;

/// <summary>Douleurs d'un jour, pour la saisie du bilan : zones déjà notées dans le bilan et crises notées à part.</summary>
public class DouleursDuJourViewModel
{
    public IReadOnlyList<ZoneDouleurViewModel> Zones { get; init; } = [];
    public IReadOnlyList<CriseDouleurViewModel> Crises { get; init; } = [];
}

public class ZoneDouleurViewModel
{
    public string TypeDouleur { get; init; } = string.Empty;
    public int Intensite { get; init; }
}

public class CriseDouleurViewModel
{
    public string TypeDouleur { get; init; } = string.Empty;
    public int Intensite { get; init; }
    /// <summary>Heure locale de la crise, « HH:mm ».</summary>
    public string Heure { get; init; } = string.Empty;
}
