using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;

namespace MonEndoVue.Server.Tests.Services;

public class BilanMesuresValidatorTests
{
    private static BilanQuotidien Bilan() => new()
    {
        CarnetSanteId = 1,
        Date = DateTime.Today,
        Mood = "Neutre",
    };

    [Fact]
    public void Valider_MesuresFacultativesNonRenseignees_EstValide()
    {
        var (estValide, erreur) = BilanMesuresValidator.Valider(Bilan());

        Assert.True(estValide);
        Assert.Null(erreur);
    }

    [Fact]
    public void Valider_MesuresAuxBornes_EstValide()
    {
        var bilan = Bilan();
        bilan.DouleurMoyenne = 10;
        bilan.StressPro = 5;
        bilan.StressPerso = 0;
        bilan.Fatigue = 5;
        bilan.Pas = 0;
        bilan.Hydratation = 10;

        Assert.True(BilanMesuresValidator.Valider(bilan).EstValide);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Valider_DouleurHorsPlage_EstInvalide(int douleur)
    {
        var bilan = Bilan();
        bilan.DouleurMoyenne = douleur;

        var (estValide, erreur) = BilanMesuresValidator.Valider(bilan);

        Assert.False(estValide);
        Assert.Contains("douleur", erreur);
    }

    [Theory]
    [InlineData(-1, null)]
    [InlineData(null, 6)]
    public void Valider_StressHorsPlage_EstInvalide(int? stressPro, int? stressPerso)
    {
        var bilan = Bilan();
        bilan.StressPro = stressPro;
        bilan.StressPerso = stressPerso;

        var (estValide, erreur) = BilanMesuresValidator.Valider(bilan);

        Assert.False(estValide);
        Assert.Contains("stress", erreur);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void Valider_FatigueHorsPlage_EstInvalide(int fatigue)
    {
        var bilan = Bilan();
        bilan.Fatigue = fatigue;

        var (estValide, erreur) = BilanMesuresValidator.Valider(bilan);

        Assert.False(estValide);
        Assert.Contains("fatigue", erreur);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100_001)]
    public void Valider_PasHorsPlage_EstInvalide(int pas)
    {
        var bilan = Bilan();
        bilan.Pas = pas;

        var (estValide, erreur) = BilanMesuresValidator.Valider(bilan);

        Assert.False(estValide);
        Assert.Contains("pas", erreur);
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(10.5)]
    public void Valider_HydratationHorsPlage_EstInvalide(double hydratation)
    {
        var bilan = Bilan();
        bilan.Hydratation = hydratation;

        var (estValide, erreur) = BilanMesuresValidator.Valider(bilan);

        Assert.False(estValide);
        Assert.Contains("hydratation", erreur);
    }
}
