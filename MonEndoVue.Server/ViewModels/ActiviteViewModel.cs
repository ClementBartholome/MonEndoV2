using System.Text.Json.Serialization;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.ViewModels;

/// <summary>Une activité physique du mois (<c>GET Activite?mois=</c>).</summary>
public class ActiviteViewModel
{
    public int Id { get; set; }

    public string Type { get; set; } = string.Empty;

    /// <summary>Date locale sans fuseau (« yyyy-MM-ddTHH:mm:ss »).</summary>
    public string Date { get; set; } = string.Empty;

    public int Duree { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NiveauActivite Niveau { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EffetActivite Effet { get; set; }

    public string? Commentaire { get; set; }
}
