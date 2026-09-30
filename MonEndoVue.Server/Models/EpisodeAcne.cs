namespace MonEndoVue.Server.Models;

/// <summary>
/// Période où l'acné est présente, du premier jour au dernier (<see cref="Fin"/> vide = en cours) : rien à noter chaque
/// jour tant qu'elle dure. Le suivi hebdomadaire (photo, intensité) reste un <see cref="SymptomeCycle"/> « Acné ».
/// </summary>
public class EpisodeAcne
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    public DateOnly Debut { get; set; }
    public DateOnly? Fin { get; set; }
}
