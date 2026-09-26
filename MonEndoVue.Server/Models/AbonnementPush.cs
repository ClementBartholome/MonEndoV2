namespace MonEndoVue.Server.Models;

/// <summary>Abonnement Web Push d'un appareil (navigateur ou app installée) rattaché à un carnet.</summary>
public class AbonnementPush
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    public CarnetSante? CarnetSante { get; set; }

    /// <summary>URL du service push de l'appareil (unique par appareil et navigateur).</summary>
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public DateTime CreeLe { get; set; }
}
