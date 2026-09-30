namespace MonEndoVue.Server.Services.Cycle;

/// <summary>Règles : jours notés consécutifs (un jour oublié toléré), du premier au dernier jour noté.</summary>
public sealed record Regles(DateOnly Debut, DateOnly Fin)
{
    public int NombreDeJours => Fin.DayNumber - Debut.DayNumber + 1;
}

/// <summary>Un cycle : du début de règles au début des suivantes (durée en jours), sans aucune prédiction.</summary>
public sealed record CycleTermine(DateOnly Debut, int JoursDeRegles, int Duree);

/// <summary>Regroupement des jours de règles notés en règles puis en cycles, pour l'historique de la page Cycle.</summary>
public static class HistoriqueCycles
{
    /// <summary>Un jour sans règles entre deux jours notés ne coupe pas les règles (même tolérance que <c>CycleDuJour</c>).</summary>
    private const int EcartMaximalDansLesRegles = 2;

    /// <summary>
    /// Au-delà, l'écart entre deux règles traduit plutôt des jours non notés qu'un cycle réel : il n'est ni listé ni
    /// compté dans la moyenne (même seuil que le « jour du cycle » de l'accueil).
    /// </summary>
    public const int DureeMaximale = 60;

    public static IReadOnlyList<Regles> Regrouper(IEnumerable<DateOnly> joursDeRegles)
    {
        var regles = new List<Regles>();
        foreach (var jour in joursDeRegles.Distinct().Order())
        {
            if (regles.Count > 0 && jour.DayNumber - regles[^1].Fin.DayNumber <= EcartMaximalDansLesRegles)
                regles[^1] = regles[^1] with { Fin = jour };
            else
                regles.Add(new Regles(jour, jour));
        }
        return regles;
    }

    /// <summary>Cycles terminés (suivis d'autres règles), du plus récent au plus ancien, au plus <paramref name="nombre"/>.</summary>
    public static IReadOnlyList<CycleTermine> Cycles(IReadOnlyList<Regles> regles, int nombre) => regles
        .Zip(regles.Skip(1), (courantes, suivantes) => new CycleTermine(
            courantes.Debut, courantes.NombreDeJours, suivantes.Debut.DayNumber - courantes.Debut.DayNumber))
        .Where(c => c.Duree <= DureeMaximale)
        .Reverse()
        .Take(nombre)
        .ToList();

    /// <summary>Durée moyenne arrondie des cycles donnés ; aucune avant deux cycles, trop peu pour parler de moyenne.</summary>
    public static int? DureeMoyenne(IReadOnlyList<CycleTermine> cycles) =>
        cycles.Count < 2 ? null : (int)Math.Round(cycles.Average(c => c.Duree), MidpointRounding.AwayFromZero);

    /// <summary>Durée moyenne arrondie des règles des cycles donnés ; aucune avant deux cycles.</summary>
    public static int? ReglesMoyenne(IReadOnlyList<CycleTermine> cycles) =>
        cycles.Count < 2 ? null : (int)Math.Round(cycles.Average(c => c.JoursDeRegles), MidpointRounding.AwayFromZero);
}
