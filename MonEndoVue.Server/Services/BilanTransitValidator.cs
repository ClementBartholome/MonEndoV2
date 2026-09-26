using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services;

/// <summary>
/// Règles de cohérence de la catégorie « Transit » du bilan quotidien.
/// Tous les champs sont facultatifs : null signifie « non renseigné ».
/// </summary>
public static class BilanTransitValidator
{
    public static readonly string[] IntensitesValides = ["Légère", "Modérée", "Forte"];

    public static (bool EstValide, string? Erreur) Valider(BilanQuotidien bilan)
    {
        if (bilan.TypeBristol.HasValue && bilan.Selles != true)
        {
            return (false, "Le type de selles (échelle de Bristol) ne peut être renseigné que s'il y a eu des selles.");
        }

        if (bilan.TypeBristol is < 1 or > 7)
        {
            return (false, "Le type de selles doit être compris entre 1 et 7 (échelle de Bristol).");
        }

        var erreurCrampes = ValiderIntensite(bilan.CrampesEstomac, bilan.IntensiteCrampes, "des crampes d'estomac");
        if (erreurCrampes != null)
        {
            return (false, erreurCrampes);
        }

        var erreurBallonnements = ValiderIntensite(bilan.Ballonnements, bilan.IntensiteBallonnements, "des ballonnements");
        if (erreurBallonnements != null)
        {
            return (false, erreurBallonnements);
        }

        return (true, null);
    }

    private static string? ValiderIntensite(bool? present, string? intensite, string libelle)
    {
        if (present == true && string.IsNullOrEmpty(intensite))
        {
            return $"L'intensité {libelle} est requise.";
        }

        if (present == true && !IntensitesValides.Contains(intensite))
        {
            return $"L'intensité {libelle} doit être Légère, Modérée ou Forte.";
        }

        if (present != true && !string.IsNullOrEmpty(intensite))
        {
            return $"L'intensité {libelle} ne peut être renseignée qu'en leur présence.";
        }

        return null;
    }
}
