using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services.Traitements;

/// <summary>Prises prévues d'un traitement un jour donné, d'après sa fréquence et ses horaires (aucun effet de bord).</summary>
public static class PlanningTraitement
{
    /// <summary>
    /// Le traitement est-il à prendre ce jour-là (dans ses dates, jour correspondant à sa fréquence) ? Un traitement arrêté
    /// sans date de fin (anciennes données) n'est plus prévu ; un traitement arrêté avec une fin reste prévu jusqu'à ce jour inclus.
    /// </summary>
    public static bool EstPrevu(Medicament traitement, DateOnly jour) =>
        (traitement.TraitementEnCours || traitement.DateFinTraitement != null) && EstPrevuDansSesDates(traitement, jour);

    /// <summary>
    /// Même règle sans la condition « en cours » : pour l'historique, un traitement arrêté depuis garde ses prises prévues
    /// jusqu'à sa date de fin. Les horaires et la fréquence sont ceux d'aujourd'hui (leurs versions passées ne sont pas gardées).
    /// </summary>
    public static bool EstPrevuDansSesDates(Medicament traitement, DateOnly jour)
    {
        if (traitement.Type != TypeTraitement.Medicamenteux) return false;

        var debut = DateOnly.FromDateTime(traitement.DateDebutTraitement);
        if (jour < debut) return false;
        if (traitement.DateFinTraitement is { } fin && jour > DateOnly.FromDateTime(fin)) return false;

        return traitement.Frequence switch
        {
            FrequencePrise.ChaqueJour => true,
            FrequencePrise.CertainsJours => traitement.JoursSemaine.HasFlag(JourDe(jour)),
            FrequencePrise.TousLesNJours => traitement.IntervalleJours is > 0 and var n && (jour.DayNumber - debut.DayNumber) % n == 0,
            _ => false,
        };
    }

    /// <summary>Heures des prises prévues ce jour-là, dans l'ordre (aucune si le traitement n'est pas prévu).</summary>
    public static IReadOnlyList<TimeOnly> HorairesDuJour(Medicament traitement, DateOnly jour) =>
        EstPrevu(traitement, jour) ? traitement.Horaires.Select(h => h.Heure).Order().ToList() : [];

    public static JoursSemaine JourDe(DateOnly jour) => jour.DayOfWeek switch
    {
        DayOfWeek.Monday => JoursSemaine.Lundi,
        DayOfWeek.Tuesday => JoursSemaine.Mardi,
        DayOfWeek.Wednesday => JoursSemaine.Mercredi,
        DayOfWeek.Thursday => JoursSemaine.Jeudi,
        DayOfWeek.Friday => JoursSemaine.Vendredi,
        DayOfWeek.Saturday => JoursSemaine.Samedi,
        _ => JoursSemaine.Dimanche,
    };
}
