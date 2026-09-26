namespace MonEndoVue.Server.Models;

/// <summary>Rappel quotidien du bilan : un réglage par carnet.</summary>
public class PreferenceRappel
{
    public int CarnetSanteId { get; set; }
    public CarnetSante? CarnetSante { get; set; }

    public bool RappelActif { get; set; }

    /// <summary>Heure locale du rappel, dans le fuseau <see cref="FuseauHoraire"/>.</summary>
    public TimeOnly HeureRappel { get; set; } = new(21, 0);

    /// <summary>Identifiant IANA du fuseau de l'utilisatrice (ex. Europe/Paris).</summary>
    public string FuseauHoraire { get; set; } = "Europe/Paris";

    /// <summary>Date locale du dernier rappel envoyé, pour n'en envoyer qu'un par jour.</summary>
    public DateOnly? DernierRappelLe { get; set; }
}
