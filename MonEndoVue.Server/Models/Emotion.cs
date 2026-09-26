namespace MonEndoVue.Server.Models;

/// <summary>
/// Émotions proposées dans le bilan quotidien : les cinq premières sont agréables, les cinq suivantes difficiles.
/// Libellés, emojis et tonalité sont définis côté client (features/bilan-quotidien/config/emotions.ts).
/// </summary>
public enum Emotion
{
    Joie,
    Calme,
    Soulagement,
    Motivation,
    Fierte,
    Tristesse,
    Anxiete,
    Irritabilite,
    Frustration,
    Decouragement,
}
