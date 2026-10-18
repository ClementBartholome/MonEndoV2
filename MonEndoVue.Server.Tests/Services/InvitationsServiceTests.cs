using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Services.Comptes;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>Ouverture progressive : codes d'invitation créés sur le serveur, vérifiés et décomptés à l'inscription.</summary>
public sealed class InvitationsServiceTests : IDisposable
{
    private readonly IdentityDeTest _identity = new() { SurInvitation = true };

    private static InscriptionDto Inscription(string email, string? code) =>
        new() { Email = email, Password = IdentityDeTest.MotDePasseValide, ConsentementDonneesSante = true, CodeInvitation = code };

    [Fact]
    public async Task Creer_NeStockeQueLEmpreinteDUnCodeLisible()
    {
        var code = await _identity.Invitations().CreerAsync(3, 30, "Bouche-à-oreille");

        Assert.Matches("^[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}$", code);
        var ligne = await _identity.Context.CodesInvitation.SingleAsync();
        Assert.Equal(InvitationsService.Hacher(code), ligne.CodeHache);
        Assert.DoesNotContain(code.Replace("-", ""), ligne.CodeHache);
        Assert.Equal((3, 0, "Bouche-à-oreille"), (ligne.UsagesMax, ligne.Usages, ligne.Libelle));
        Assert.Equal(IdentityDeTest.Maintenant.UtcDateTime.AddDays(30), ligne.ExpireLe);
    }

    [Fact]
    public async Task Inscrire_AvecUnCodeValable_CreeLeCompteEtCompteUnUsage()
    {
        var code = await _identity.Invitations().CreerAsync(2, 30, null);

        // Saisie tolérante : minuscules, espaces et tirets n'importent pas.
        var resultat = await _identity.Comptes().InscrireAsync(Inscription("invitee@local", code.ToLowerInvariant().Replace("-", " ")), CancellationToken.None);

        Assert.Equal(IssueCompte.Acceptee, resultat.Issue);
        Assert.NotNull(await _identity.UserManager.FindByEmailAsync("invitee@local"));
        Assert.Equal(1, (await _identity.Context.CodesInvitation.SingleAsync()).Usages);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AAAA-BBBB-CCCC")]
    public async Task Inscrire_SansCodeValable_RefuseSansRienCreerNiEnvoyer(string? code)
    {
        var resultat = await _identity.Comptes().InscrireAsync(Inscription("sans-code@local", code), CancellationToken.None);

        Assert.Equal(IssueCompte.Invalide, resultat.Issue);
        Assert.Equal([InvitationsService.MessageCodeInvalide], resultat.Erreurs);
        Assert.Null(await _identity.UserManager.FindByEmailAsync("sans-code@local"));
        Assert.Empty(_identity.Emails.Planifies);
    }

    [Fact]
    public async Task Inscrire_CodeEpuise_EstRefuse()
    {
        var code = await _identity.Invitations().CreerAsync(1, null, null);
        await _identity.Comptes().InscrireAsync(Inscription("premiere@local", code), CancellationToken.None);

        var resultat = await _identity.Comptes().InscrireAsync(Inscription("seconde@local", code), CancellationToken.None);

        Assert.Equal(IssueCompte.Invalide, resultat.Issue);
        Assert.Null(await _identity.UserManager.FindByEmailAsync("seconde@local"));
    }

    [Fact]
    public async Task Inscrire_CodeExpire_EstRefuse()
    {
        // Créé il y a 31 jours, valable 30 jours.
        var ancien = new InvitationsService(_identity.Context, new HorlogeFixe(IdentityDeTest.Maintenant.AddDays(-31)), NullLogger<InvitationsService>.Instance);
        var code = await ancien.CreerAsync(5, 30, null);

        var resultat = await _identity.Comptes().InscrireAsync(Inscription("tardive@local", code), CancellationToken.None);

        Assert.Equal(IssueCompte.Invalide, resultat.Issue);
    }

    [Fact]
    public async Task Inscrire_AdresseDejaInscrite_NeConsommePasDUsage()
    {
        await _identity.CreerUtilisatrice("deja@local");
        var code = await _identity.Invitations().CreerAsync(1, 30, null);

        var resultat = await _identity.Comptes().InscrireAsync(Inscription("deja@local", code), CancellationToken.None);

        Assert.Equal(IssueCompte.Acceptee, resultat.Issue);
        Assert.Equal(0, (await _identity.Context.CodesInvitation.SingleAsync()).Usages);
    }

    [Fact]
    public async Task InscriptionsOuvertes_LeCodeNEstPasDemande()
    {
        _identity.SurInvitation = false;

        var resultat = await _identity.Comptes().InscrireAsync(Inscription("libre@local", null), CancellationToken.None);

        Assert.Equal(IssueCompte.Acceptee, resultat.Issue);
    }

    public void Dispose() => _identity.Dispose();
}
