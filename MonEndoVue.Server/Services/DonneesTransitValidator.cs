using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services;

/// <summary>
/// Règles de validation métier pour le suivi du transit intestinal (issue #3).
/// </summary>
public static class DonneesTransitValidator
{
    private static readonly string[] IntensitesValides = ["Légère", "Modérée", "Forte"];

    public static (bool EstValide, string? Erreur) Valider(DonneesTransit donnees)
    {
        if (donnees.TypeBristol.HasValue && !donnees.Selles)
        {
            return (false, "Le type Bristol ne peut être renseigné que si Selles est à true.");
        }

        if (donnees.TypeBristol.HasValue && (donnees.TypeBristol < 1 || donnees.TypeBristol > 7))
        {
            return (false, "Le type Bristol doit être compris entre 1 et 7.");
        }

        if (donnees.CrampesEstomac && string.IsNullOrEmpty(donnees.IntensiteCrampes))
        {
            return (false, "L'intensité des Crampes d'estomac est requise lorsque CrampesEstomac est à true.");
        }

        if (donnees.CrampesEstomac && !IntensitesValides.Contains(donnees.IntensiteCrampes))
        {
            return (false, "L'intensité des Crampes d'estomac doit être Légère, Modérée ou Forte.");
        }

        if (!donnees.CrampesEstomac && !string.IsNullOrEmpty(donnees.IntensiteCrampes))
        {
            return (false, "L'intensité des Crampes d'estomac ne peut être renseignée que si CrampesEstomac est à true.");
        }

        if (donnees.Ballonnements && string.IsNullOrEmpty(donnees.IntensiteBallonnements))
        {
            return (false, "L'intensité des Ballonnements est requise lorsque Ballonnements est à true.");
        }

        if (donnees.Ballonnements && !IntensitesValides.Contains(donnees.IntensiteBallonnements))
        {
            return (false, "L'intensité des Ballonnements doit être Légère, Modérée ou Forte.");
        }

        if (!donnees.Ballonnements && !string.IsNullOrEmpty(donnees.IntensiteBallonnements))
        {
            return (false, "L'intensité des Ballonnements ne peut être renseignée que si Ballonnements est à true.");
        }

        return (true, null);
    }
}
