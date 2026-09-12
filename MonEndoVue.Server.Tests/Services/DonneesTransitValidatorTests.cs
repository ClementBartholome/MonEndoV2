using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using Xunit;

namespace MonEndoVue.Server.Tests.Services;

public class DonneesTransitValidatorTests
{
    private static DonneesTransit BaseValid() => new()
    {
        CarnetSanteId = 1,
        Date = DateTime.Today,
        Selles = false,
        CrampesEstomac = false,
        Ballonnements = false
    };

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(7)]
    public void EstValide_AvecTypeBristolDansLaPlage_RetourneTrue(int typeBristol)
    {
        var donnees = BaseValid();
        donnees.Selles = true;
        donnees.TypeBristol = typeBristol;

        var (estValide, _) = DonneesTransitValidator.Valider(donnees);

        Assert.True(estValide);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(-1)]
    public void EstValide_AvecTypeBristolHorsPlage_RetourneFalse(int typeBristol)
    {
        var donnees = BaseValid();
        donnees.Selles = true;
        donnees.TypeBristol = typeBristol;

        var (estValide, erreur) = DonneesTransitValidator.Valider(donnees);

        Assert.False(estValide);
        Assert.Contains("Bristol", erreur);
    }

    [Fact]
    public void EstValide_SellesFalse_TypeBristolNonRenseigne_RetourneTrue()
    {
        var donnees = BaseValid();
        donnees.Selles = true;
        donnees.TypeBristol = null; // option "ne pas renseigner"

        var (estValide, _) = DonneesTransitValidator.Valider(donnees);

        Assert.True(estValide);
    }

    [Fact]
    public void EstValide_SellesFalse_AvecTypeBristolRenseigne_RetourneFalse()
    {
        var donnees = BaseValid();
        donnees.Selles = false;
        donnees.TypeBristol = 3;

        var (estValide, erreur) = DonneesTransitValidator.Valider(donnees);

        Assert.False(estValide);
        Assert.Contains("Selles", erreur);
    }

    [Theory]
    [InlineData("Légère")]
    [InlineData("Modérée")]
    [InlineData("Forte")]
    public void EstValide_CrampesAvecIntensiteValide_RetourneTrue(string intensite)
    {
        var donnees = BaseValid();
        donnees.CrampesEstomac = true;
        donnees.IntensiteCrampes = intensite;

        var (estValide, _) = DonneesTransitValidator.Valider(donnees);

        Assert.True(estValide);
    }

    [Fact]
    public void EstValide_CrampesTrue_SansIntensite_RetourneFalse()
    {
        var donnees = BaseValid();
        donnees.CrampesEstomac = true;
        donnees.IntensiteCrampes = null;

        var (estValide, erreur) = DonneesTransitValidator.Valider(donnees);

        Assert.False(estValide);
        Assert.Contains("Crampes", erreur);
    }

    [Fact]
    public void EstValide_CrampesTrue_AvecIntensiteInvalide_RetourneFalse()
    {
        var donnees = BaseValid();
        donnees.CrampesEstomac = true;
        donnees.IntensiteCrampes = "Extreme";

        var (estValide, erreur) = DonneesTransitValidator.Valider(donnees);

        Assert.False(estValide);
        Assert.Contains("Crampes", erreur);
    }

    [Fact]
    public void EstValide_CrampesFalse_AvecIntensiteRenseignee_RetourneFalse()
    {
        var donnees = BaseValid();
        donnees.CrampesEstomac = false;
        donnees.IntensiteCrampes = "Légère";

        var (estValide, erreur) = DonneesTransitValidator.Valider(donnees);

        Assert.False(estValide);
        Assert.Contains("Crampes", erreur);
    }

    [Theory]
    [InlineData("Légère")]
    [InlineData("Modérée")]
    [InlineData("Forte")]
    public void EstValide_BallonnementsAvecIntensiteValide_RetourneTrue(string intensite)
    {
        var donnees = BaseValid();
        donnees.Ballonnements = true;
        donnees.IntensiteBallonnements = intensite;

        var (estValide, _) = DonneesTransitValidator.Valider(donnees);

        Assert.True(estValide);
    }

    [Fact]
    public void EstValide_BallonnementsTrue_SansIntensite_RetourneFalse()
    {
        var donnees = BaseValid();
        donnees.Ballonnements = true;
        donnees.IntensiteBallonnements = null;

        var (estValide, erreur) = DonneesTransitValidator.Valider(donnees);

        Assert.False(estValide);
        Assert.Contains("Ballonnements", erreur);
    }

    [Fact]
    public void EstValide_BallonnementsFalse_AvecIntensiteRenseignee_RetourneFalse()
    {
        var donnees = BaseValid();
        donnees.Ballonnements = false;
        donnees.IntensiteBallonnements = "Forte";

        var (estValide, erreur) = DonneesTransitValidator.Valider(donnees);

        Assert.False(estValide);
        Assert.Contains("Ballonnements", erreur);
    }

    [Fact]
    public void EstValide_DonneesEntierementVides_RetourneTrue()
    {
        var donnees = BaseValid();

        var (estValide, erreur) = DonneesTransitValidator.Valider(donnees);

        Assert.True(estValide);
        Assert.Null(erreur);
    }
}
