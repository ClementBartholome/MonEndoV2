using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace MonEndoVue.Server.Models;

public class ApplicationUser : IdentityUser
{
    /// <summary>Ancien jeton de renouvellement unique, en clair : remplacé par <see cref="SessionAppareil"/> et lu seulement pour convertir les sessions ouvertes avant la 1.5.0 (colonnes à supprimer en 1.6.0).</summary>
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public CarnetSante? CarnetSante { get; set; }

    /// <summary>Date (UTC) du consentement explicite au traitement des données de santé ; null tant qu'il n'est pas donné.</summary>
    public DateTime? ConsentementDonneesSanteLe { get; set; }

    /// <summary>Version de la politique de confidentialité acceptée (voir <c>PolitiqueConfidentialite.Version</c>).</summary>
    [MaxLength(20)]
    public string? VersionPolitiqueAcceptee { get; set; }

    /// <summary>
    /// Dernière ouverture ou prolongation de session (UTC) : un compte sans activité depuis 2 ans est supprimé
    /// (<c>ComptesInactifsService</c>). Renseignée pour les comptes existants par la migration qui l'ajoute.
    /// </summary>
    public DateTime? DerniereActiviteLe { get; set; }
}