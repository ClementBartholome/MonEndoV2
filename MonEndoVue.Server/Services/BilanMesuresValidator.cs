using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services;

/// <summary>
/// Plages des mesures du bilan quotidien. La douleur est obligatoire ; stress, fatigue, pas et hydratation sont
/// facultatifs (null = non renseigné) mais bornés lorsqu'ils sont renseignés.
/// </summary>
public static class BilanMesuresValidator
{
    public const int DouleurMax = 10;
    public const int EchelleMax = 5;
    public const int PasMax = 100_000;
    public const double HydratationMax = 10;

    public static (bool EstValide, string? Erreur) Valider(BilanQuotidien bilan)
    {
        if (bilan.DouleurMoyenne is < 0 or > DouleurMax)
        {
            return (false, $"La douleur doit être comprise entre 0 et {DouleurMax}.");
        }

        if (HorsEchelle(bilan.StressPro) || HorsEchelle(bilan.StressPerso))
        {
            return (false, $"Le stress doit être compris entre 0 et {EchelleMax}.");
        }

        if (HorsEchelle(bilan.Fatigue))
        {
            return (false, $"La fatigue doit être comprise entre 0 et {EchelleMax}.");
        }

        if (bilan.Pas is < 0 or > PasMax)
        {
            return (false, $"Le nombre de pas doit être compris entre 0 et {PasMax}.");
        }

        if (bilan.Hydratation is < 0 or > HydratationMax)
        {
            return (false, $"L'hydratation doit être comprise entre 0 et {HydratationMax} litres.");
        }

        return (true, null);
    }

    private static bool HorsEchelle(int? valeur) => valeur is < 0 or > EchelleMax;
}
