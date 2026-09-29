using System.Text.Json.Serialization;

namespace MonEndoVue.Server.Dto;

/// <summary>Début (et fin éventuelle, vide = en cours) d'un épisode d'acné : création ou correction.</summary>
public class EpisodeAcneDto
{
    [JsonRequired]
    public DateOnly Debut { get; set; }

    public DateOnly? Fin { get; set; }
}

/// <summary>Dernier jour d'un épisode en cours (« Ça s'est calmé »).</summary>
public class FinEpisodeAcneDto
{
    [JsonRequired]
    public DateOnly Fin { get; set; }
}
