namespace MonEndoVue.Server.Models;

/// <summary>Liaison d'un carnet à l'agenda Google de l'utilisatrice (lecture seule). Une seule par carnet.</summary>
public class LiaisonAgenda
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    public CarnetSante? CarnetSante { get; set; }

    /// <summary>Jeton d'actualisation Google, chiffré par Data Protection : jamais en clair en base ni dans les logs.</summary>
    public string JetonActualisationProtege { get; set; } = string.Empty;

    public DateTime LieeLe { get; set; }
}
