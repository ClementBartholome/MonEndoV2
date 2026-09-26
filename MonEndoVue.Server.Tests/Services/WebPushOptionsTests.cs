using MonEndoVue.Server.Services.WebPush;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

public class WebPushOptionsTests
{
    private static WebPushOptions Valides()
    {
        var (publique, privee) = PushDeTest.GenererClesP256();
        return new WebPushOptions { Subject = "mailto:test@local", PublicKey = publique, PrivateKey = privee };
    }

    [Theory]
    [InlineData("mailto:test@local")]
    [InlineData("https://monendo.example")]
    public void Erreur_ConfigurationComplete_AucuneErreur(string sujet)
    {
        var options = Valides();
        options.Subject = sujet;

        Assert.Null(options.Erreur());
        Assert.True(options.EstConfiguree);
    }

    [Fact]
    public void Erreur_SectionAbsente_LeSignale()
    {
        Assert.Contains("absente", new WebPushOptions().Erreur());
    }

    [Theory]
    [InlineData("test@local")]
    [InlineData("")]
    public void Erreur_SujetSansMailto_LeSignale(string sujet)
    {
        var options = Valides();
        options.Subject = sujet;

        Assert.Contains("Subject", options.Erreur());
        Assert.False(options.EstConfiguree);
    }

    [Fact]
    public void Erreur_CleTronquee_IndiqueLaLongueurSansLaValeur()
    {
        var options = Valides();
        options.PrivateKey = options.PrivateKey![..42];

        var erreur = options.Erreur();

        Assert.Equal("WebPush:PrivateKey doit faire 43 caractères (actuellement 42)", erreur);
        Assert.DoesNotContain(options.PrivateKey, erreur);
    }

    [Fact]
    public void Erreur_ClePubliqueManquante_LeSignale()
    {
        var options = Valides();
        options.PublicKey = null;

        Assert.Equal("WebPush:PublicKey doit faire 87 caractères (actuellement 0)", options.Erreur());
    }
}
