using MonEndoVue.Server.Services.Accueil;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>Jour de règles et jour du cycle déduits des jours notés, sans prédiction.</summary>
public class CycleDuJourTests
{
    private static readonly DateOnly Jour = new(2026, 9, 28);

    private static DateOnly[] Jours(params int[] decalages) => decalages.Select(d => Jour.AddDays(d)).ToArray();

    [Fact]
    public void Calculer_AucunJourNote_Inconnu()
    {
        Assert.Equal(CycleDuJour.Inconnu, CycleDuJour.Calculer([], Jour));
    }

    [Fact]
    public void Calculer_DeuxiemeJourDeRegles_EnReglesJour2()
    {
        Assert.Equal(new CycleDuJour(true, 2, 2), CycleDuJour.Calculer(Jours(-1, 0), Jour));
    }

    [Fact]
    public void Calculer_UnJourOublieDansLesRegles_NeCoupePasLesRegles()
    {
        Assert.Equal(new CycleDuJour(true, 4, 4), CycleDuJour.Calculer(Jours(-3, -1, 0), Jour));
    }

    [Fact]
    public void Calculer_ReglesTerminees_JourDuCycleSeulement()
    {
        Assert.Equal(new CycleDuJour(false, null, 12), CycleDuJour.Calculer(Jours(-11, -10, -9), Jour));
    }

    [Fact]
    public void Calculer_ReglesPrecedentesSeparees_CompteDepuisLesDernieres()
    {
        // Deux épisodes séparés de 28 jours : le cycle repart du dernier.
        Assert.Equal(new CycleDuJour(false, null, 3), CycleDuJour.Calculer(Jours(-30, -29, -2, -1), Jour));
    }

    [Fact]
    public void Calculer_DernierDebutTropAncien_Inconnu()
    {
        Assert.Equal(CycleDuJour.Inconnu, CycleDuJour.Calculer(Jours(-CycleDuJour.DureeMaximaleCycle), Jour));
    }

    [Fact]
    public void Calculer_JoursFutursIgnores()
    {
        Assert.Equal(new CycleDuJour(false, null, 5), CycleDuJour.Calculer(Jours(-4, 2), Jour));
    }
}
