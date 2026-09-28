using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Dto;

/// <summary>Création ou modification d'un traitement (carnet déduit de la session, jamais reçu).</summary>
public class TraitementDto
{
    [Required]
    public string Nom { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TypeTraitement Type { get; set; }

    /// <summary>Dose par prise (texte libre : « 1 comprimé », « 2 mg ») ; enregistrée dans Posologie.</summary>
    public string? Dose { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FrequencePrise Frequence { get; set; }

    /// <summary>Noms des jours (« Lundi »…) pour une fréquence « certains jours ».</summary>
    public List<string> JoursSemaine { get; set; } = [];

    public int? IntervalleJours { get; set; }

    /// <summary>Heures locales des prises (« 08:00:00 »).</summary>
    public List<TimeOnly> Horaires { get; set; } = [];

    public DateOnly DateDebut { get; set; }

    public DateOnly? DateFin { get; set; }
}

/// <summary>Réponse à une prise : faite ou ignorée, à l'heure locale de l'utilisatrice.</summary>
public class PriseDto
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StatutPrise Statut { get; set; }

    /// <summary>Horaire prévu auquel la prise répond ; absent pour une prise « au besoin ».</summary>
    public TimeOnly? HeurePrevue { get; set; }

    /// <summary>Date et heure locales, sans fuseau.</summary>
    public DateTime Date { get; set; }
}

/// <summary>Séance d'un soin non médicamenteux (kiné, ostéo…).</summary>
public class SeanceDto
{
    public DateTime Date { get; set; }

    [Range(1, 600)]
    public int? Duree { get; set; }

    [MaxLength(500)]
    public string? Commentaire { get; set; }
}
