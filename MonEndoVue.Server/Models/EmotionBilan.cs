using System.Text.Json.Serialization;

namespace MonEndoVue.Server.Models;

/// <summary>Émotion déclarée dans un bilan quotidien (une ligne par émotion, table EmotionsBilan).</summary>
public class EmotionBilan
{
    public int Id { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Emotion Emotion { get; set; }
}
