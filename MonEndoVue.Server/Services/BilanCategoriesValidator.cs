using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services;

/// <summary>
/// Règles de cohérence des catégories facultatives du bilan quotidien (transit complété, urinaire, saignements hors règles,
/// nuit et journée, rapports). Tous les champs sont facultatifs : null signifie « non renseigné ». Une intensité n'existe que
/// si le symptôme est présent, et chaque choix est limité à ses valeurs connues.
/// </summary>
public static class BilanCategoriesValidator
{
    /// <summary>Mêmes intensités que le transit.</summary>
    public static readonly string[] Intensites = BilanTransitValidator.IntensitesValides;

    public static readonly string[] Abondances = ["Traces", "Legers", "Abondants"];
    public static readonly string[] Nuits = ["Bonne", "Moyenne", "Difficile"];
    public static readonly string[] Limitations = ["PasLimitee", "PeuLimitee", "TresLimitee"];
    public static readonly string[] ReponsesRapport = ["Oui", "Non", "PasDeRapport"];

    public static (bool EstValide, string? Erreur) Valider(BilanQuotidien bilan)
    {
        var erreur =
            Intensite(bilan.DouleurSelle, bilan.IntensiteDouleurSelle, "de la douleur en allant à la selle")
            ?? Intensite(bilan.DouleurUriner, bilan.IntensiteDouleurUriner, "de la douleur en urinant")
            ?? Abondance(bilan)
            ?? Choix(bilan.Nuit, Nuits, "La qualité de la nuit")
            ?? Choix(bilan.LimitationJournee, Limitations, "La limitation de la journée")
            ?? Choix(bilan.DouleurRapport, ReponsesRapport, "La réponse sur les rapports");
        return erreur is null ? (true, null) : (false, erreur);
    }

    private static string? Intensite(bool? present, string? intensite, string libelle)
    {
        if (present == true && string.IsNullOrEmpty(intensite)) return $"L'intensité {libelle} est requise.";
        if (present == true && !Intensites.Contains(intensite)) return $"L'intensité {libelle} doit être Légère, Modérée ou Forte.";
        if (present != true && !string.IsNullOrEmpty(intensite)) return $"L'intensité {libelle} ne peut être renseignée qu'en présence du symptôme.";
        return null;
    }

    /// <summary>L'abondance est facultative, mais n'a de sens que s'il y a eu des saignements hors règles.</summary>
    private static string? Abondance(BilanQuotidien bilan)
    {
        if (string.IsNullOrEmpty(bilan.AbondanceSaignementsHorsRegles)) return null;
        if (bilan.SaignementsHorsRegles != true) return "L'abondance des saignements hors règles ne peut être renseignée qu'en leur présence.";
        return Choix(bilan.AbondanceSaignementsHorsRegles, Abondances, "L'abondance des saignements hors règles");
    }

    private static string? Choix(string? valeur, string[] valides, string libelle) =>
        string.IsNullOrEmpty(valeur) || valides.Contains(valeur) ? null : $"{libelle} n'est pas une valeur reconnue.";
}
