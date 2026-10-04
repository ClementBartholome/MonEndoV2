using System.ComponentModel.DataAnnotations;

namespace MonEndoVue.Server.Models;

/// <summary>
/// Session d'un appareil : une ligne par appareil connecté, pour qu'une connexion ailleurs ne ferme pas les autres. Le jeton de
/// renouvellement n'est jamais stocké en clair, seulement son empreinte SHA-256.
/// </summary>
public class SessionAppareil
{
    public int Id { get; set; }

    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }

    /// <summary>Empreinte du jeton de renouvellement en cours (contrôle d'accès concurrent : deux renouvellements simultanés ne gagnent pas ensemble).</summary>
    [MaxLength(64)]
    public required string JetonHache { get; set; }

    /// <summary>
    /// Empreinte du jeton précédent, encore accepté un temps limité après sa rotation : l'appareil dont la réponse de
    /// renouvellement s'est perdue (application coupée par le système) présente l'ancien jeton et ne doit pas être déconnecté.
    /// </summary>
    [MaxLength(64)]
    public string? JetonPrecedentHache { get; set; }

    public DateTime? RotationLe { get; set; }

    public DateTime CreeLe { get; set; }
    public DateTime DerniereUtilisationLe { get; set; }
    public DateTime ExpireLe { get; set; }
}
