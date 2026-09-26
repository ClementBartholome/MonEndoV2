using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;

namespace MonEndoVue.Server.Tests.Services;

public class BilanTransitValidatorTests
{
    private static BilanQuotidien Bilan() => new()
    {
        CarnetSanteId = 1,
        Date = DateTime.Today,
        Mood = "Neutre",
    };

    [Fact]
    public void Valider_TransitNonRenseigne_EstValide()
    {
        var (estValide, erreur) = BilanTransitValidator.Valider(Bilan());

        Assert.True(estValide);
        Assert.Null(erreur);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(7)]
    public void Valider_SellesAvecTypeBristolDansLaPlage_EstValide(int typeBristol)
    {
        var bilan = Bilan();
        bilan.Selles = true;
        bilan.TypeBristol = typeBristol;

        Assert.True(BilanTransitValidator.Valider(bilan).EstValide);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(-1)]
    public void Valider_TypeBristolHorsPlage_EstInvalide(int typeBristol)
    {
        var bilan = Bilan();
        bilan.Selles = true;
        bilan.TypeBristol = typeBristol;

        var (estValide, erreur) = BilanTransitValidator.Valider(bilan);

        Assert.False(estValide);
        Assert.Contains("Bristol", erreur);
    }

    [Fact]
    public void Valider_SellesSansTypeBristol_EstValide()
    {
        var bilan = Bilan();
        bilan.Selles = true; // option « ne pas renseigner » le type

        Assert.True(BilanTransitValidator.Valider(bilan).EstValide);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public void Valider_TypeBristolSansSelles_EstInvalide(bool? selles)
    {
        var bilan = Bilan();
        bilan.Selles = selles;
        bilan.TypeBristol = 4;

        Assert.False(BilanTransitValidator.Valider(bilan).EstValide);
    }

    [Theory]
    [InlineData("Légère")]
    [InlineData("Modérée")]
    [InlineData("Forte")]
    public void Valider_CrampesAvecIntensiteValide_EstValide(string intensite)
    {
        var bilan = Bilan();
        bilan.CrampesEstomac = true;
        bilan.IntensiteCrampes = intensite;

        Assert.True(BilanTransitValidator.Valider(bilan).EstValide);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Sévère")]
    public void Valider_CrampesSansIntensiteValide_EstInvalide(string? intensite)
    {
        var bilan = Bilan();
        bilan.CrampesEstomac = true;
        bilan.IntensiteCrampes = intensite;

        var (estValide, erreur) = BilanTransitValidator.Valider(bilan);

        Assert.False(estValide);
        Assert.Contains("crampes", erreur);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public void Valider_IntensiteCrampesSansCrampes_EstInvalide(bool? crampes)
    {
        var bilan = Bilan();
        bilan.CrampesEstomac = crampes;
        bilan.IntensiteCrampes = "Forte";

        Assert.False(BilanTransitValidator.Valider(bilan).EstValide);
    }

    [Theory]
    [InlineData("Légère")]
    [InlineData("Modérée")]
    [InlineData("Forte")]
    public void Valider_BallonnementsAvecIntensiteValide_EstValide(string intensite)
    {
        var bilan = Bilan();
        bilan.Ballonnements = true;
        bilan.IntensiteBallonnements = intensite;

        Assert.True(BilanTransitValidator.Valider(bilan).EstValide);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Intense")]
    public void Valider_BallonnementsSansIntensiteValide_EstInvalide(string? intensite)
    {
        var bilan = Bilan();
        bilan.Ballonnements = true;
        bilan.IntensiteBallonnements = intensite;

        var (estValide, erreur) = BilanTransitValidator.Valider(bilan);

        Assert.False(estValide);
        Assert.Contains("ballonnements", erreur);
    }

    [Fact]
    public void Valider_IntensiteBallonnementsSansBallonnements_EstInvalide()
    {
        var bilan = Bilan();
        bilan.Ballonnements = false;
        bilan.IntensiteBallonnements = "Légère";

        Assert.False(BilanTransitValidator.Valider(bilan).EstValide);
    }

    [Fact]
    public void Valider_TransitCompletCoherent_EstValide()
    {
        var bilan = Bilan();
        bilan.Selles = true;
        bilan.TypeBristol = 3;
        bilan.CrampesEstomac = true;
        bilan.IntensiteCrampes = "Légère";
        bilan.Ballonnements = false;

        Assert.True(BilanTransitValidator.Valider(bilan).EstValide);
    }
}
