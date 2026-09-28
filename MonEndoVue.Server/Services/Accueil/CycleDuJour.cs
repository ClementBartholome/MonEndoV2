namespace MonEndoVue.Server.Services.Accueil;

/// <summary>Position dans le cycle un jour donné, déduite des jours de règles notés (aucune prédiction).</summary>
public sealed record CycleDuJour(bool EnRegles, int? JourDeRegles, int? JourDuCycle)
{
    /// <summary>Au-delà, le dernier début de règles est trop ancien pour parler de « jour du cycle ».</summary>
    public const int DureeMaximaleCycle = 60;

    /// <summary>Un jour sans règles entre deux jours notés ne coupe pas les règles (jour oublié).</summary>
    private const int EcartMaximalDansLesRegles = 2;

    public static readonly CycleDuJour Inconnu = new(false, null, null);

    /// <summary>
    /// Début des dernières règles (jours notés consécutifs, un oubli d'un jour toléré) au plus tard le jour donné.
    /// En règles si ce jour est noté ; jour du cycle compté depuis ce début, s'il date de moins de 60 jours.
    /// </summary>
    public static CycleDuJour Calculer(IEnumerable<DateOnly> joursDeRegles, DateOnly jour)
    {
        var jours = joursDeRegles.Where(j => j <= jour).Distinct().OrderByDescending(j => j).ToList();
        if (jours.Count == 0) return Inconnu;

        var debut = jours[0];
        foreach (var precedent in jours.Skip(1))
        {
            if (debut.DayNumber - precedent.DayNumber > EcartMaximalDansLesRegles) break;
            debut = precedent;
        }

        var jourDuCycle = jour.DayNumber - debut.DayNumber + 1;
        if (jourDuCycle > DureeMaximaleCycle) return Inconnu;

        var enRegles = jours[0] == jour;
        return new CycleDuJour(enRegles, enRegles ? jourDuCycle : null, jourDuCycle);
    }
}
