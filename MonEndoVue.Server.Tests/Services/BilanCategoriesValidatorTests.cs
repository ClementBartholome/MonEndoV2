using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;

namespace MonEndoVue.Server.Tests.Services;

public class BilanCategoriesValidatorTests
{
    private static BilanQuotidien Bilan() => new() { CarnetSanteId = 1, Date = DateTime.Today, Mood = "Neutre" };

    [Fact]
    public void Valider_AucuneCategorieRenseignee_EstValide()
    {
        var (estValide, erreur) = BilanCategoriesValidator.Valider(Bilan());

        Assert.True(estValide);
        Assert.Null(erreur);
    }

    [Fact]
    public void Valider_ToutesLesCategoriesCoherentes_EstValide()
    {
        var bilan = Bilan();
        bilan.DouleurSelle = true;
        bilan.IntensiteDouleurSelle = "Légère";
        bilan.Nausees = false;
        bilan.SangSelles = false;
        bilan.DouleurUriner = true;
        bilan.IntensiteDouleurUriner = "Modérée";
        bilan.EnviesUrinaires = true;
        bilan.DifficulteVider = false;
        bilan.SangUrines = false;
        bilan.SaignementsHorsRegles = true;
        bilan.AbondanceSaignementsHorsRegles = "Traces";
        bilan.Nuit = "Moyenne";
        bilan.ReveilsDouleur = true;
        bilan.LimitationJournee = "PeuLimitee";
        bilan.AbsenceTravail = false;
        bilan.ActiviteAnnulee = true;
        bilan.DouleurRapport = "PasDeRapport";

        Assert.True(BilanCategoriesValidator.Valider(bilan).EstValide);
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(true, "")]
    [InlineData(true, "Énorme")]
    [InlineData(false, "Forte")]
    [InlineData(null, "Légère")]
    public void Valider_IntensiteDeLaDouleurEnAllantALaSelle_DoitSuivreLaPresence(bool? presente, string? intensite)
    {
        var bilan = Bilan();
        bilan.DouleurSelle = presente;
        bilan.IntensiteDouleurSelle = intensite;

        Assert.False(BilanCategoriesValidator.Valider(bilan).EstValide);
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(true, "Insupportable")]
    [InlineData(false, "Légère")]
    [InlineData(null, "Forte")]
    public void Valider_IntensiteDeLaDouleurEnUrinant_DoitSuivreLaPresence(bool? presente, string? intensite)
    {
        var bilan = Bilan();
        bilan.DouleurUriner = presente;
        bilan.IntensiteDouleurUriner = intensite;

        Assert.False(BilanCategoriesValidator.Valider(bilan).EstValide);
    }

    [Fact]
    public void Valider_SaignementsSansAbondance_EstValide()
    {
        var bilan = Bilan();
        bilan.SaignementsHorsRegles = true;

        Assert.True(BilanCategoriesValidator.Valider(bilan).EstValide);
    }

    [Theory]
    [InlineData(false, "Traces")]
    [InlineData(null, "Legers")]
    [InlineData(true, "Torrentiels")]
    public void Valider_AbondanceDesSaignementsHorsRegles_SeulementSiPresentsEtConnue(bool? saignements, string abondance)
    {
        var bilan = Bilan();
        bilan.SaignementsHorsRegles = saignements;
        bilan.AbondanceSaignementsHorsRegles = abondance;

        Assert.False(BilanCategoriesValidator.Valider(bilan).EstValide);
    }

    [Theory]
    [InlineData("Excellente")]
    [InlineData("bonne")]
    public void Valider_NuitInconnue_EstRefusee(string nuit)
    {
        var bilan = Bilan();
        bilan.Nuit = nuit;

        Assert.False(BilanCategoriesValidator.Valider(bilan).EstValide);
    }

    [Fact]
    public void Valider_LimitationDeLaJourneeInconnue_EstRefusee()
    {
        var bilan = Bilan();
        bilan.LimitationJournee = "Moyennement";

        Assert.False(BilanCategoriesValidator.Valider(bilan).EstValide);
    }

    [Fact]
    public void Valider_ReponseSurLesRapportsInconnue_EstRefusee()
    {
        var bilan = Bilan();
        bilan.DouleurRapport = "Parfois";

        Assert.False(BilanCategoriesValidator.Valider(bilan).EstValide);
    }
}
