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

/// <summary>Réglage du rappel quotidien ; l'heure est au format HH:mm dans le fuseau IANA indiqué.</summary>
public class PreferenceRappelDto
{
    public bool RappelActif { get; set; }

    [Required]
    public string HeureRappel { get; set; } = "21:00";

    [Required]
    public string FuseauHoraire { get; set; } = "Europe/Paris";
}
