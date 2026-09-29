using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Dto;

/// <summary>Saisie d'une activité physique (carnet déduit de la session, jamais reçu).</summary>
public class ActiviteDto
{
    [Required, MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    /// <summary>Date et heure locales, sans fuseau.</summary>
    [JsonRequired]
    public DateTime Date { get; set; }

    /// <summary>Durée en minutes.</summary>
    [JsonRequired, Range(1, 600)]
    public int Duree { get; set; }

    [JsonRequired]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NiveauActivite Niveau { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EffetActivite Effet { get; set; }

    [MaxLength(500)]
    public string? Commentaire { get; set; }
}
