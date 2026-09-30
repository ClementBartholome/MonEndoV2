namespace MonEndoVue.Server.Models;

public class DonneesActivitePhysique
{
    public int Id { get; set; }
    public int CarnetSanteId { get; set; }
    public string TypeActivite { get; set; }
    public DateTime Date { get; set; }
    public int Duree { get; set; }

    /// <summary>
    /// Ancienne intensité de 1 à 10, encore écrite (douce 2, modérée 5, soutenue 8) pour qu'une image antérieure et
    /// l'export PDF restent cohérents. Le suivi utilise <see cref="NiveauIntensite"/>.
    /// </summary>
    public int Intensite { get; set; }

    /// <summary>Intensité ressentie sur 3 niveaux (<see cref="NiveauActivite"/>) ; reprise de l'ancienne échelle par la migration.</summary>
    public int? NiveauIntensite { get; set; }

    /// <summary>Effet sur la douleur (<see cref="EffetActivite"/>) ; 0 = non renseigné.</summary>
    public int EffetDouleur { get; set; }

    public string? Commentaire { get; set; }
}

public enum NiveauActivite
{
    Douce = 1,
    Moderee = 2,
    Soutenue = 3,
}

public enum EffetActivite
{
    NonRenseigne = 0,
    Soulagee = 1,
    Pareille = 2,
    PlusForte = 3,
}
