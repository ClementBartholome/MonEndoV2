using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services;

/// <summary>
/// Règles de la partie « humeur » du bilan quotidien : une à trois émotions distinctes, ou l'ancienne humeur
/// (Mood) d'un bilan saisi avant les émotions ; commentaire de longueur limitée.
/// </summary>
public static class BilanHumeurValidator
{
    public const int EmotionsMax = 3;
    public const int CommentaireMax = 1000;

    public static (bool EstValide, string? Erreur) Valider(BilanQuotidien bilan)
    {
        var emotions = bilan.Emotions.Select(e => e.Emotion).ToList();

        if (emotions.Count == 0 && string.IsNullOrWhiteSpace(bilan.Mood))
        {
            return (false, "Choisis au moins une émotion.");
        }

        if (emotions.Count > EmotionsMax)
        {
            return (false, $"Tu peux choisir {EmotionsMax} émotions au maximum.");
        }

        if (emotions.Any(e => !Enum.IsDefined(e)))
        {
            return (false, "Émotion inconnue.");
        }

        if (emotions.Distinct().Count() != emotions.Count)
        {
            return (false, "Chaque émotion ne peut être choisie qu'une fois.");
        }

        if (bilan.Commentaire?.Length > CommentaireMax)
        {
            return (false, $"Le commentaire ne peut pas dépasser {CommentaireMax} caractères.");
        }

        return (true, null);
    }
}
