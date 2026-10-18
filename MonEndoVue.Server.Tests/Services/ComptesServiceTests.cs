using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services.Comptes;
using MonEndoVue.Server.Services.Sessions;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>Inscription confirmée par e-mail, connexion, mot de passe oublié : sans énumération des adresses inscrites.</summary>
public sealed class ComptesServiceTests : IDisposable
{
    private readonly IdentityDeTest _identity = new();

    private static InscriptionDto Inscription(string email, string motDePasse = IdentityDeTest.MotDePasseValide) =>
        new() { Email = email, Password = motDePasse, ConsentementDonneesSante = true };

    private async Task<ApplicationUser> Inscrite(string email)
    {
        Assert.Equal(IssueCompte.Acceptee, (await _identity.Comptes().InscrireAsync(Inscription(email), CancellationToken.None)).Issue);
        return (await _identity.UserManager.FindByEmailAsync(email))!;
    }

    [Fact]
    public async Task Inscrire_EnvoieUnLienDeConfirmationVersLAdresseDeLApplication()
    {
        await Inscrite("nouvelle@local");

        var message = Assert.Single(_identity.Emails.Pour("nouvelle@local"));
        Assert.Contains("Confirme ton adresse", message.Sujet);
        Assert.Contains($"{IdentityDeTest.AdresseApplication}/confirmer-adresse#email=", message.Texte);
    }

    [Fact]
    public async Task Inscrire_AdresseDejaInscrite_MemeReponseEtUnEmailQuiPrevientLaPersonne()
    {
        await _identity.CreerUtilisatrice("deja@local");

        var resultat = await _identity.Comptes().InscrireAsync(Inscription("deja@local", "AutreMotDePasse2!"), CancellationToken.None);

        Assert.Equal(IssueCompte.Acceptee, resultat.Issue);
        Assert.Equal(1, await _identity.Context.Users.CountAsync(u => u.Email == "deja@local"));
        Assert.Contains("déjà un compte", Assert.Single(_identity.Emails.Pour("deja@local")).Sujet);
        // Le mot de passe du compte existant n'a pas changé.
        Assert.True(await _identity.UserManager.CheckPasswordAsync((await _identity.UserManager.FindByEmailAsync("deja@local"))!, IdentityDeTest.MotDePasseValide));
    }

    [Fact]
    public async Task Inscrire_MotDePasseFaible_LeDitMemeSiLAdresseExisteDeja()
    {
        await _identity.CreerUtilisatrice("deja@local");

        var nouvelle = await _identity.Comptes().InscrireAsync(Inscription("inconnue@local", "abc"), CancellationToken.None);
        var existante = await _identity.Comptes().InscrireAsync(Inscription("deja@local", "abc"), CancellationToken.None);

        // Même réponse dans les deux cas : la règle du mot de passe, jamais « adresse déjà utilisée ».
        Assert.Equal(IssueCompte.Invalide, nouvelle.Issue);
        Assert.Equal(nouvelle.Erreurs, existante.Erreurs);
        Assert.DoesNotContain(existante.Erreurs, e => e.Contains("utilisée"));
    }

    [Fact]
    public async Task Inscrire_SansRelaisDEmails_RefuseSansRienCreer()
    {
        _identity.Emails.Disponible = false;

        var resultat = await _identity.Comptes().InscrireAsync(Inscription("sans-relais@local"), CancellationToken.None);

        Assert.Equal(IssueCompte.EmailIndisponible, resultat.Issue);
        Assert.Null(await _identity.UserManager.FindByEmailAsync("sans-relais@local"));
    }

    [Fact]
    public async Task Confirmer_AvecLeJetonDuLien_ConfirmeLAdresse()
    {
        await Inscrite("a-confirmer@local");
        var (email, jeton) = FauxEnvoiEmail.LireLien(_identity.Emails.Pour("a-confirmer@local")[0]);

        var resultat = await _identity.Comptes().ConfirmerAdresseAsync(email, jeton);

        Assert.Equal(IssueCompte.Acceptee, resultat.Issue);
        Assert.True((await _identity.UserManager.FindByEmailAsync("a-confirmer@local"))!.EmailConfirmed);
    }

    [Theory]
    [InlineData("a-confirmer@local", "jeton-invente")]
    [InlineData("inconnue@local", "jeton-invente")]
    public async Task Confirmer_JetonOuAdresseInvalide_UnSeulMessage(string email, string jeton)
    {
        await Inscrite("a-confirmer@local");

        var resultat = await _identity.Comptes().ConfirmerAdresseAsync(email, jeton);

        Assert.Equal(IssueCompte.Invalide, resultat.Issue);
        Assert.Equal([ComptesService.MessageLienInvalide], resultat.Erreurs);
    }

    [Fact]
    public async Task Connexion_AdresseNonConfirmee_DiteSeulementAvecLeBonMotDePasse()
    {
        await Inscrite("pas-confirmee@local");

        var bonMotDePasse = await _identity.Comptes().VerifierConnexionAsync("pas-confirmee@local", IdentityDeTest.MotDePasseValide);
        var mauvais = await _identity.Comptes().VerifierConnexionAsync("pas-confirmee@local", "Mauvais1!");
        var inconnue = await _identity.Comptes().VerifierConnexionAsync("inconnue@local", IdentityDeTest.MotDePasseValide);

        Assert.Equal(IssueConnexion.AdresseNonConfirmee, bonMotDePasse.Issue);
        Assert.Null(bonMotDePasse.User);
        Assert.Equal(IssueConnexion.Refusee, mauvais.Issue);
        Assert.Equal(IssueConnexion.Refusee, inconnue.Issue);
    }

    [Fact]
    public async Task Connexion_AdresseNonConfirmee_LeControleurRepond403AvecUnCodeSansOuvrirDeSession()
    {
        await Inscrite("pas-confirmee@local");
        var controller = _identity.CreerController();

        var resultat = await controller.Login(new IdentifiantsDto { Email = "pas-confirmee@local", Password = IdentityDeTest.MotDePasseValide });

        var refus = Assert.IsType<ObjectResult>(resultat);
        Assert.Equal(403, refus.StatusCode);
        Assert.Contains(AccountController.CodeAdresseNonConfirmee, refus.Value!.ToString());
        Assert.DoesNotContain("refreshToken", controller.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public async Task Renvoyer_UnLienAuPlusParMinuteEtRienPourUneAdresseInconnueOuConfirmee()
    {
        await Inscrite("renvoi@local");
        await _identity.CreerUtilisatrice("confirmee@local");

        await _identity.Comptes().RenvoyerConfirmationAsync("renvoi@local");
        await _identity.Comptes().RenvoyerConfirmationAsync("inconnue@local");
        await _identity.Comptes().RenvoyerConfirmationAsync("confirmee@local");

        // L'inscription vient d'envoyer un lien : le renvoi dans la minute n'en ajoute pas.
        Assert.Single(_identity.Emails.Pour("renvoi@local"));
        Assert.Empty(_identity.Emails.Pour("inconnue@local"));
        Assert.Empty(_identity.Emails.Pour("confirmee@local"));
    }

    [Fact]
    public async Task MotDePasseOublie_LienPourUnCompteConfirmeConfirmationSinonRienPourUneInconnue()
    {
        await _identity.CreerUtilisatrice("confirmee@local");

        var connue = await _identity.Comptes().DemanderReinitialisationAsync("confirmee@local");
        var inconnue = await _identity.Comptes().DemanderReinitialisationAsync("inconnue@local");

        Assert.Equal((IssueCompte.Acceptee, IssueCompte.Acceptee), (connue.Issue, inconnue.Issue));
        Assert.Contains("/reinitialiser-mot-de-passe#email=", Assert.Single(_identity.Emails.Pour("confirmee@local")).Texte);
        Assert.Empty(_identity.Emails.Pour("inconnue@local"));
    }

    [Fact]
    public async Task MotDePasseOublie_SansRelaisDEmails_Indisponible()
    {
        _identity.Emails.Disponible = false;

        Assert.Equal(IssueCompte.EmailIndisponible, (await _identity.Comptes().DemanderReinitialisationAsync("x@local")).Issue);
    }

    [Fact]
    public async Task Reinitialiser_ChangeLeMotDePasseFermeToutesLesSessionsEtLeveLeVerrou()
    {
        var user = await _identity.CreerUtilisatrice("oubli@local");
        var sessions = new SessionsService(_identity.Context, new MonEndoVue.Server.Services.TokenService(
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                [MonEndoVue.Server.Services.CleSignatureJwt.Reglage] = "cle-de-test-uniquement-pour-les-tests-unitaires",
            }).Build()), new HorlogeFixe(IdentityDeTest.Maintenant));
        await sessions.OuvrirAsync(user.Id);
        await _identity.UserManager.SetLockoutEndDateAsync(user, IdentityDeTest.Maintenant.AddYears(1));
        await _identity.Comptes().DemanderReinitialisationAsync("oubli@local");
        var (email, jeton) = FauxEnvoiEmail.LireLien(_identity.Emails.Pour("oubli@local")[0]);

        var resultat = await _identity.Comptes().ReinitialiserAsync(
            new ReinitialisationMotDePasseDto { Email = email, Jeton = jeton, NouveauMotDePasse = "NouveauMotDePasse2!" }, CancellationToken.None);

        Assert.Equal(IssueCompte.Acceptee, resultat.Issue);
        var apres = (await _identity.UserManager.FindByEmailAsync("oubli@local"))!;
        Assert.True(await _identity.UserManager.CheckPasswordAsync(apres, "NouveauMotDePasse2!"));
        Assert.False(await _identity.UserManager.IsLockedOutAsync(apres));
        Assert.Empty(await _identity.Context.SessionsAppareil.Where(s => s.UserId == user.Id).ToListAsync());
    }

    [Fact]
    public async Task Reinitialiser_JetonDeConfirmation_NeSertPasAChangerLeMotDePasse()
    {
        await Inscrite("detournement@local");
        var (email, jeton) = FauxEnvoiEmail.LireLien(_identity.Emails.Pour("detournement@local")[0]);

        var resultat = await _identity.Comptes().ReinitialiserAsync(
            new ReinitialisationMotDePasseDto { Email = email, Jeton = jeton, NouveauMotDePasse = "NouveauMotDePasse2!" }, CancellationToken.None);

        Assert.Equal(IssueCompte.Invalide, resultat.Issue);
        Assert.Equal([ComptesService.MessageLienInvalide], resultat.Erreurs);
    }

    [Fact]
    public async Task Reinitialiser_MotDePasseFaible_DitLaRegle()
    {
        await _identity.CreerUtilisatrice("faible@local");
        await _identity.Comptes().DemanderReinitialisationAsync("faible@local");
        var (email, jeton) = FauxEnvoiEmail.LireLien(_identity.Emails.Pour("faible@local")[0]);

        var resultat = await _identity.Comptes().ReinitialiserAsync(
            new ReinitialisationMotDePasseDto { Email = email, Jeton = jeton, NouveauMotDePasse = "abc" }, CancellationToken.None);

        Assert.Equal(IssueCompte.Invalide, resultat.Issue);
        Assert.Contains(resultat.Erreurs, e => e.Contains("huit caractères"));
    }

    [Fact]
    public async Task PreDetournement_MotDePasseOublieSurUnCompteNonConfirme_EcarteLeMotDePasseDeLIntrus()
    {
        // Quelqu'un crée un compte avec l'adresse d'une autre personne, sans pouvoir le confirmer.
        await _identity.Comptes().InscrireAsync(Inscription("victime@local", "MotDePasseIntrus1!"), CancellationToken.None);
        // Plus d’une minute plus tard (nouveau cache : le délai entre deux e-mails est écoulé).
        _identity.Emails.Planifies.Clear();

        var oubli = await _identity.Comptes(new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions())).DemanderReinitialisationAsync("victime@local");

        // Un lien de réinitialisation, jamais un lien de confirmation qui activerait le compte avec le mot de passe de l'intrus.
        Assert.Equal(IssueCompte.Acceptee, oubli.Issue);
        var message = Assert.Single(_identity.Emails.Pour("victime@local"));
        Assert.Contains("/reinitialiser-mot-de-passe#", message.Texte);
        var (email, jeton) = FauxEnvoiEmail.LireLien(message);
        await _identity.Comptes().ReinitialiserAsync(
            new ReinitialisationMotDePasseDto { Email = email, Jeton = jeton, NouveauMotDePasse = "MotDePasseVictime2!" }, CancellationToken.None);

        Assert.Equal(IssueConnexion.Refusee, (await _identity.Comptes().VerifierConnexionAsync("victime@local", "MotDePasseIntrus1!")).Issue);
        Assert.Equal(IssueConnexion.Acceptee, (await _identity.Comptes().VerifierConnexionAsync("victime@local", "MotDePasseVictime2!")).Issue);
    }

    [Fact]
    public async Task PreDetournement_NouvelleInscriptionSurUnCompteNonConfirme_RemplaceLeMotDePasseEtInvalideLesAnciensLiens()
    {
        await _identity.Comptes().InscrireAsync(Inscription("victime@local", "MotDePasseIntrus1!"), CancellationToken.None);
        var (email, ancienJeton) = FauxEnvoiEmail.LireLien(_identity.Emails.Pour("victime@local")[0]);

        var reinscription = await _identity.Comptes().InscrireAsync(Inscription("victime@local", "MotDePasseVictime2!"), CancellationToken.None);

        Assert.Equal(IssueCompte.Acceptee, reinscription.Issue);
        Assert.Equal(1, await _identity.Context.Users.CountAsync(u => u.Email == "victime@local"));
        // Le lien envoyé à la première inscription ne vaut plus (l'empreinte de sécurité a changé avec le mot de passe).
        Assert.Equal(IssueCompte.Invalide, (await _identity.Comptes().ConfirmerAdresseAsync(email, ancienJeton)).Issue);
        var user = (await _identity.UserManager.FindByEmailAsync("victime@local"))!;
        Assert.False(await _identity.UserManager.CheckPasswordAsync(user, "MotDePasseIntrus1!"));
        Assert.True(await _identity.UserManager.CheckPasswordAsync(user, "MotDePasseVictime2!"));
    }

    public void Dispose() => _identity.Dispose();
}
