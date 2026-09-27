using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace MonEndoVue.Server.Models;

public class ApplicationUser : IdentityUser
{
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public CarnetSante? CarnetSante { get; set; }

    /// <summary>Date (UTC) du consentement explicite au traitement des données de santé ; null tant qu'il n'est pas donné.</summary>
    public DateTime? ConsentementDonneesSanteLe { get; set; }

    /// <summary>Version de la politique de confidentialité acceptée (voir <c>PolitiqueConfidentialite.Version</c>).</summary>
    [MaxLength(20)]
    public string? VersionPolitiqueAcceptee { get; set; }
}