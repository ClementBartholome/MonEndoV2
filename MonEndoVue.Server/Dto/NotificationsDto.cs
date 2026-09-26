using System.ComponentModel.DataAnnotations;

namespace MonEndoVue.Server.Dto;

/// <summary>Abonnement Web Push d'un appareil (<c>PushSubscription.toJSON()</c> côté navigateur).</summary>
public class AbonnementPushDto
{
    [Required, MaxLength(450)]
    public string Endpoint { get; set; } = string.Empty;

    [Required]
    public string P256dh { get; set; } = string.Empty;

    [Required]
    public string Auth { get; set; } = string.Empty;
}

public class DesabonnementPushDto
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;
}

/// <summary>
/// Réglage d'un rappel. <see cref="Type"/> (BilanQuotidien, SuiviAcne) et <see cref="EstHebdomadaire"/> sont renseignés
/// en lecture ; l'heure est au format HH:mm dans le fuseau IANA indiqué ; le jour (0 = dimanche … 6 = samedi) ne concerne
/// que les rappels hebdomadaires.
/// </summary>
public class RappelDto
{
    public string Type { get; set; } = string.Empty;
    public bool EstHebdomadaire { get; set; }
    public bool Actif { get; set; }

    [Required]
    public string Heure { get; set; } = "21:00";

    public int? JourSemaine { get; set; }

    [Required]
    public string FuseauHoraire { get; set; } = "Europe/Paris";
}
