namespace MonEndoVue.Server.Models;

/// <summary>Fréquence d'un traitement (inspirée de l'app Santé d'Apple). Les traitements d'avant la 1.3.0 sont « au besoin ».</summary>
public enum FrequencePrise
{
    AuBesoin = 0,
    ChaqueJour = 1,
    CertainsJours = 2,
    TousLesNJours = 3,
}

/// <summary>Jours de la semaine d'une fréquence « certains jours », combinables.</summary>
[Flags]
public enum JoursSemaine
{
    Aucun = 0,
    Lundi = 1,
    Mardi = 2,
    Mercredi = 4,
    Jeudi = 8,
    Vendredi = 16,
    Samedi = 32,
    Dimanche = 64,
}

/// <summary>Réponse à une prise : prise, ou volontairement ignorée (notée pour le suivi).</summary>
public enum StatutPrise
{
    Pris = 0,
    Ignore = 1,
}

/// <summary>Horaire d'une prise prévue (type possédé par <see cref="Medicament"/>, table HorairesPrise).</summary>
public class HorairePrise
{
    public int Id { get; set; }
    public TimeOnly Heure { get; set; }
}
