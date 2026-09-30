namespace MonEndoVue.Server.Models;

public class Medicament
{
    public int Id { get; set; }
    public CarnetSante? CarnetSante { get; set; }
    public int CarnetSanteId { get; set; }
    public string Nom { get; set; }
    public TypeTraitement Type { get; set; }
    public string? Posologie { get; set; }
    public bool TraitementEnCours { get; set; }
    public DateTime DateDebutTraitement { get; set; }
    public DateTime? DateFinTraitement { get; set; }

    /// <summary>Quand le prendre (défaut : au besoin, comme tous les traitements d'avant la 1.3.0).</summary>
    public FrequencePrise Frequence { get; set; }

    /// <summary>Jours de prise si <see cref="FrequencePrise.CertainsJours"/>.</summary>
    public JoursSemaine JoursSemaine { get; set; }

    /// <summary>Tous les N jours à partir du début du traitement si <see cref="FrequencePrise.TousLesNJours"/>.</summary>
    public int? IntervalleJours { get; set; }

    /// <summary>Heures des prises d'un jour prévu (aucune pour « au besoin »).</summary>
    public List<HorairePrise> Horaires { get; set; } = [];
}