using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services.Traitements;

/// <summary>Règles d'un traitement saisi ; renvoie un message en français, ou null s'il est valide.</summary>
public static class TraitementValidator
{
    public const int NomMax = 100;
    public const int DoseMax = 100;
    public const int HorairesMax = 6;
    public const int IntervalleMin = 2;
    public const int IntervalleMax = 30;

    public static string? Valider(TraitementDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nom)) return "Le nom du traitement est obligatoire.";
        if (dto.Nom.Trim().Length > NomMax) return $"Le nom ne peut pas dépasser {NomMax} caractères.";
        if (dto.Dose?.Trim().Length > DoseMax) return $"La dose ne peut pas dépasser {DoseMax} caractères.";
        if (!Enum.IsDefined(dto.Frequence)) return "Fréquence inconnue.";
        if (dto.DateFin is { } fin && fin < dto.DateDebut) return "La fin du traitement ne peut pas précéder son début.";

        if (dto.Frequence == FrequencePrise.AuBesoin) return null;

        if (dto.Horaires.Count == 0) return "Indique au moins un horaire de prise.";
        if (dto.Horaires.Count > HorairesMax) return $"{HorairesMax} horaires au plus.";
        if (dto.Horaires.Distinct().Count() != dto.Horaires.Count) return "Un même horaire est indiqué deux fois.";

        return dto.Frequence switch
        {
            FrequencePrise.CertainsJours when Jours(dto) is null => "Jour de la semaine inconnu.",
            FrequencePrise.CertainsJours when Jours(dto) == JoursSemaine.Aucun => "Choisis au moins un jour.",
            FrequencePrise.TousLesNJours when dto.IntervalleJours is not (>= IntervalleMin and <= IntervalleMax) =>
                $"L'intervalle doit être compris entre {IntervalleMin} et {IntervalleMax} jours.",
            _ => null,
        };
    }

    /// <summary>Jours cochés combinés ; null si un nom de jour est inconnu.</summary>
    public static JoursSemaine? Jours(TraitementDto dto)
    {
        var jours = JoursSemaine.Aucun;
        foreach (var nom in dto.JoursSemaine)
        {
            if (!Enum.TryParse<JoursSemaine>(nom, out var jour) || jour == JoursSemaine.Aucun || !Enum.IsDefined(jour)) return null;
            jours |= jour;
        }
        return jours;
    }
}
