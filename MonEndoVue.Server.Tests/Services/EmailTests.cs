using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MonEndoVue.Server.Services.Email;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

public sealed class EmailTests
{
    private static EmailOptions Valide() => new()
    {
        Hote = "smtp-relay.exemple.test", Port = 587, Utilisateur = "identifiant@smtp.exemple.test", MotDePasse = "cle-de-test",
        Expediteur = "no-reply@monendoapp.fr", AdresseApplication = "https://monendoapp.fr",
    };

    [Fact]
    public void Erreur_ConfigurationComplete_EstNulle() => Assert.Null(Valide().Erreur());

    [Fact]
    public void Erreur_SectionAbsente_LeDit() => Assert.Contains("absente", new EmailOptions().Erreur());

    [Theory]
    [InlineData("Hote")]
    [InlineData("Utilisateur")]
    [InlineData("MotDePasse")]
    [InlineData("Expediteur")]
    [InlineData("AdresseApplication")]
    public void Erreur_UnChampManquant_LeNomme(string champ)
    {
        var options = Valide();
        typeof(EmailOptions).GetProperty(champ)!.SetValue(options, null);

        Assert.Contains(champ, options.Erreur());
    }

    [Theory]
    [InlineData("pas-une-adresse")]
    [InlineData("Nom <no-reply@monendoapp.fr>")]
    public void Erreur_ExpediteurQuiNEstPasUneAdresse_EstRefuse(string expediteur)
    {
        var options = Valide();
        options.Expediteur = expediteur;

        Assert.Contains("Expediteur", options.Erreur());
    }

    [Theory]
    [InlineData("http://monendoapp.fr")]
    [InlineData("monendoapp.fr")]
    public void Erreur_AdresseDeLApplicationNonHttps_EstRefusee(string adresse)
    {
        var options = Valide();
        options.AdresseApplication = adresse;

        Assert.Contains("AdresseApplication", options.Erreur());
    }

    [Fact]
    public void Erreur_ApplicationLocaleEnDeveloppement_EstAcceptee()
    {
        var options = Valide();
        options.AdresseApplication = "https://localhost:5173";

        Assert.Null(options.Erreur());
    }

    [Fact]
    public void Erreur_NeCiteJamaisLeMotDePasse()
    {
        var options = Valide();
        options.Port = 0;

        Assert.DoesNotContain("cle-de-test", options.Erreur());
    }

    [Fact]
    public async Task Smtp_SansConfiguration_NEnvoieRienEtLeDit()
    {
        var envoi = new EnvoiEmailSmtp(Options.Create(new EmailOptions()), NullLogger<EnvoiEmailSmtp>.Instance);

        var issue = await envoi.EnvoyerAsync(new MessageEmail("a@exemple.test", "Sujet", "Texte", "<p>Texte</p>"));

        Assert.False(envoi.Disponible);
        Assert.Equal(IssueEnvoiEmail.Desactive, issue);
    }

    [Fact]
    public void Liens_PortentLeJetonDansLeFragmentEtNonDansLaRequete()
    {
        var lien = ModelesEmail.LienReinitialisation("https://monendoapp.fr/", "a+b@exemple.test", "jeton/avec+des=symboles&autres");

        var uri = new Uri(lien);
        Assert.Equal("/reinitialiser-mot-de-passe", uri.AbsolutePath);
        Assert.Equal(string.Empty, uri.Query);
        Assert.StartsWith("#email=", uri.Fragment);
        var message = ModelesEmail.ReinitialisationMotDePasse("a+b@exemple.test", lien);
        Assert.Equal(("a+b@exemple.test", "jeton/avec+des=symboles&autres"), FauxEnvoiEmail.LireLien(message));
    }

    [Fact]
    public void Messages_ContiennentLeLienEnTexteEtEnHtmlSansDonneeDeSante()
    {
        var lien = ModelesEmail.LienConfirmation("https://monendoapp.fr", "a@exemple.test", "jeton");
        var messages = new[]
        {
            ModelesEmail.Confirmation("a@exemple.test", lien),
            ModelesEmail.ReinitialisationMotDePasse("a@exemple.test", lien),
            ModelesEmail.CompteExistant("a@exemple.test", "https://monendoapp.fr/login", "https://monendoapp.fr/mot-de-passe-oublie"),
        };

        foreach (var message in messages)
        {
            Assert.Contains("https://monendoapp.fr", message.Texte);
            Assert.Contains("https://monendoapp.fr", message.Html);
            Assert.Contains("MonEndo", message.Sujet);
            foreach (var mot in new[] { "douleur", "règles", "endométriose", "traitement", "bilan" })
            {
                Assert.DoesNotContain(mot, message.Texte, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Html_EchappeLesValeursInserees()
    {
        var message = ModelesEmail.Confirmation("a@exemple.test", "https://monendoapp.fr/confirmer-adresse#email=a&jeton=\"><script>alert(1)</script>");

        Assert.DoesNotContain("<script>", message.Html);
    }
}
