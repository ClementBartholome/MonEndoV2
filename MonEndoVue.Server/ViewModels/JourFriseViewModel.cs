namespace MonEndoVue.Server.ViewModels;

/// <summary>Un jour de la frise (voir <c>FriseService</c>).</summary>
public class JourFriseViewModel
{
    /// <summary>AAAA-MM-JJ.</summary>
    public required string Jour { get; init; }
    /// <summary>Douleur la plus forte notée ce jour-là (0 à 10, bilan ou entrée de douleur) ; null si rien n'est noté.</summary>
    public int? Douleur { get; init; }
    public bool Regles { get; init; }
}
