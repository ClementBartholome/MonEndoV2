namespace MonEndoVue.Server.Models;

/// <summary>Types de rappels proposés ; chacun a sa règle (<c>Services/WebPush/Rappels</c>).</summary>
public enum TypeRappel
{
    BilanQuotidien,
    SuiviAcne,
}

/// <summary>Réglage d'un rappel pour un carnet : un seul par type et par carnet.</summary>
public class Rappel
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    public CarnetSante? CarnetSante { get; set; }

    public TypeRappel Type { get; set; }
    public bool Actif { get; set; }

    /// <summary>Heure locale du rappel, dans le fuseau <see cref="FuseauHoraire"/>.</summary>
    public TimeOnly Heure { get; set; }

    /// <summary>Jour du rappel pour un rappel hebdomadaire ; null pour un rappel quotidien.</summary>
    public DayOfWeek? JourSemaine { get; set; }

    /// <summary>Identifiant IANA du fuseau de l'utilisatrice (ex. Europe/Paris).</summary>
    public string FuseauHoraire { get; set; } = "Europe/Paris";

    /// <summary>Date locale du dernier envoi, pour n'envoyer qu'un rappel par jour.</summary>
    public DateOnly? DernierEnvoiLe { get; set; }
}
