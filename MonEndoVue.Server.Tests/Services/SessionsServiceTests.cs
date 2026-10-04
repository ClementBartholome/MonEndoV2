using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Sessions;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>Sessions par appareil : un jeton de renouvellement par appareil, haché, qui tourne sans fermer les autres.</summary>
public sealed class SessionsServiceTests : IDisposable
{
    private readonly IdentityDeTest _identity = new();
    private readonly HorlogeMobile _horloge = new(IdentityDeTest.Maintenant);

    private SessionsService Service() => new(_identity.Context,
        new TokenService(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { [CleSignatureJwt.Reglage] = "cle-de-test-uniquement-pour-les-tests-unitaires" }).Build()),
        _horloge);

    [Fact]
    public async Task Ouvrir_NeStockeQueLEmpreinteDuJeton()
    {
        var user = await _identity.CreerUtilisatrice("hachage@local");

        var session = await Service().OuvrirAsync(user.Id);

        var ligne = await _identity.Context.SessionsAppareil.SingleAsync();
        Assert.NotEqual(session.Jeton, ligne.JetonHache);
        Assert.Equal(SessionsService.Hacher(session.Jeton), ligne.JetonHache);
        Assert.Equal(_horloge.GetUtcNow().UtcDateTime + SessionsService.DureeSession, session.Expiration);
    }

    [Fact]
    public async Task DeuxAppareils_ChacunRenouvelleSaSessionSansFermerLAutre()
    {
        var user = await _identity.CreerUtilisatrice("deux-appareils@local");
        var telephone = await Service().OuvrirAsync(user.Id);
        var ordinateur = await Service().OuvrirAsync(user.Id);

        var r1 = await Service().RenouvelerAsync(telephone.Jeton);
        var r2 = await Service().RenouvelerAsync(ordinateur.Jeton);
        var r3 = await Service().RenouvelerAsync(r1.Session!.Jeton);

        Assert.All(new[] { r1, r2, r3 }, r => Assert.Equal(IssueRenouvellement.Renouvelee, r.Issue));
        Assert.Equal(2, await _identity.Context.SessionsAppareil.CountAsync());
    }

    [Fact]
    public async Task Renouveler_TourneLeJetonEtGlisseLExpiration()
    {
        var user = await _identity.CreerUtilisatrice("rotation@local");
        var session = await Service().OuvrirAsync(user.Id);
        _horloge.Avancer(TimeSpan.FromHours(30));

        var resultat = await Service().RenouvelerAsync(session.Jeton);

        Assert.Equal(user.Id, resultat.UserId);
        Assert.NotEqual(session.Jeton, resultat.Session!.Jeton);
        Assert.Equal(_horloge.GetUtcNow().UtcDateTime + SessionsService.DureeSession, resultat.Session.Expiration);
    }

    [Fact]
    public async Task Renouveler_AncienJetonDansLaTolerance_NeDeconnectePas()
    {
        // Réponse de renouvellement perdue : l'appareil présente encore l'ancien jeton.
        var user = await _identity.CreerUtilisatrice("reponse-perdue@local");
        var session = await Service().OuvrirAsync(user.Id);
        await Service().RenouvelerAsync(session.Jeton);
        _horloge.Avancer(SessionsService.ToleranceReponsePerdue - TimeSpan.FromMinutes(1));

        var resultat = await Service().RenouvelerAsync(session.Jeton);

        Assert.Equal(IssueRenouvellement.Renouvelee, resultat.Issue);
        var suite = await Service().RenouvelerAsync(resultat.Session!.Jeton);
        Assert.Equal(IssueRenouvellement.Renouvelee, suite.Issue);
    }

    [Fact]
    public async Task Renouveler_AncienJetonApresLaTolerance_EstRefuse()
    {
        var user = await _identity.CreerUtilisatrice("trop-tard@local");
        var session = await Service().OuvrirAsync(user.Id);
        await Service().RenouvelerAsync(session.Jeton);
        _horloge.Avancer(SessionsService.ToleranceReponsePerdue + TimeSpan.FromMinutes(1));

        var resultat = await Service().RenouvelerAsync(session.Jeton);

        Assert.Equal(IssueRenouvellement.Inconnue, resultat.Issue);
    }

    [Fact]
    public async Task Renouveler_SessionExpiree_EstRefuseeEtSupprimee()
    {
        var user = await _identity.CreerUtilisatrice("expiree@local");
        var session = await Service().OuvrirAsync(user.Id);
        _horloge.Avancer(SessionsService.DureeSession + TimeSpan.FromMinutes(1));

        var resultat = await Service().RenouvelerAsync(session.Jeton);

        Assert.Equal(IssueRenouvellement.Expiree, resultat.Issue);
        Assert.Empty(await _identity.Context.SessionsAppareil.ToListAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("jeton-inconnu")]
    public async Task Renouveler_JetonVideOuInconnu_EstRefuse(string jeton)
    {
        var user = await _identity.CreerUtilisatrice("inconnu@local");
        user.RefreshToken = string.Empty;
        await _identity.UserManager.UpdateAsync(user);

        Assert.Equal(IssueRenouvellement.Inconnue, (await Service().RenouvelerAsync(jeton)).Issue);
    }

    [Fact]
    public async Task Renouveler_SessionOuverteAvantLaMiseAJour_EstConvertie()
    {
        var user = await _identity.CreerUtilisatrice("ancienne@local");
        user.RefreshToken = "ancien-jeton-en-clair";
        user.RefreshTokenExpiryTime = _horloge.GetUtcNow().UtcDateTime.AddHours(5);
        await _identity.UserManager.UpdateAsync(user);

        var resultat = await Service().RenouvelerAsync("ancien-jeton-en-clair");

        Assert.Equal(IssueRenouvellement.Renouvelee, resultat.Issue);
        Assert.Equal(1, await _identity.Context.SessionsAppareil.CountAsync());
        var apres = (await _identity.UserManager.FindByIdAsync(user.Id))!;
        Assert.Empty(apres.RefreshToken);
        Assert.Equal(IssueRenouvellement.Inconnue, (await Service().RenouvelerAsync("ancien-jeton-en-clair")).Issue);
    }

    [Fact]
    public async Task Renouveler_AncienneSessionExpiree_EstRefusee()
    {
        var user = await _identity.CreerUtilisatrice("ancienne-expiree@local");
        user.RefreshToken = "ancien-jeton-expire";
        user.RefreshTokenExpiryTime = _horloge.GetUtcNow().UtcDateTime.AddMinutes(-1);
        await _identity.UserManager.UpdateAsync(user);

        Assert.Equal(IssueRenouvellement.Expiree, (await Service().RenouvelerAsync("ancien-jeton-expire")).Issue);
        Assert.Empty(await _identity.Context.SessionsAppareil.ToListAsync());
    }

    [Fact]
    public async Task Revoquer_FermeSeulementLAppareilConcerne()
    {
        var user = await _identity.CreerUtilisatrice("sortie-appareil@local");
        var telephone = await Service().OuvrirAsync(user.Id);
        var ordinateur = await Service().OuvrirAsync(user.Id);

        await Service().RevoquerAsync(telephone.Jeton);

        Assert.Equal(IssueRenouvellement.Inconnue, (await Service().RenouvelerAsync(telephone.Jeton)).Issue);
        Assert.Equal(IssueRenouvellement.Renouvelee, (await Service().RenouvelerAsync(ordinateur.Jeton)).Issue);
    }

    [Fact]
    public async Task RevoquerAutres_GardeLAppareilCourant()
    {
        var user = await _identity.CreerUtilisatrice("autres@local");
        var courant = await Service().OuvrirAsync(user.Id);
        var autre = await Service().OuvrirAsync(user.Id);
        var voisine = await _identity.CreerUtilisatrice("voisine@local");
        var sessionVoisine = await Service().OuvrirAsync(voisine.Id);

        await Service().RevoquerAutresAsync(user.Id, courant.Jeton);

        Assert.Equal(IssueRenouvellement.Renouvelee, (await Service().RenouvelerAsync(courant.Jeton)).Issue);
        Assert.Equal(IssueRenouvellement.Inconnue, (await Service().RenouvelerAsync(autre.Jeton)).Issue);
        Assert.Equal(IssueRenouvellement.Renouvelee, (await Service().RenouvelerAsync(sessionVoisine.Jeton)).Issue);
    }

    [Fact]
    public async Task Ouvrir_AuDelaDuNombreMaxDAppareils_RetireLePlusAncien()
    {
        var user = await _identity.CreerUtilisatrice("beaucoup@local");
        var premiere = await Service().OuvrirAsync(user.Id);
        for (var i = 1; i < SessionsService.AppareilsMax; i++)
        {
            _horloge.Avancer(TimeSpan.FromMinutes(1));
            await Service().OuvrirAsync(user.Id);
        }

        _horloge.Avancer(TimeSpan.FromMinutes(1));
        await Service().OuvrirAsync(user.Id);

        Assert.Equal(SessionsService.AppareilsMax, await _identity.Context.SessionsAppareil.CountAsync());
        Assert.Equal(IssueRenouvellement.Inconnue, (await Service().RenouvelerAsync(premiere.Jeton)).Issue);
    }

    [Fact]
    public async Task Controleur_DeuxConnexions_LesDeuxAppareilsRestentConnectes()
    {
        await _identity.CreerUtilisatrice("multi@local");
        var telephone = _identity.CreerController();
        var ordinateur = _identity.CreerController();
        await telephone.Login(new IdentifiantsDto { Email = "multi@local", Password = IdentityDeTest.MotDePasseValide });
        await ordinateur.Login(new IdentifiantsDto { Email = "multi@local", Password = IdentityDeTest.MotDePasseValide });

        foreach (var appareil in new[] { telephone, ordinateur })
        {
            var renouvellement = _identity.CreerController();
            renouvellement.ControllerContext.HttpContext.Request.Headers.Cookie = $"refreshToken={JetonPose(appareil)}";
            Assert.IsType<OkObjectResult>(await renouvellement.RefreshToken());
        }
    }

    [Fact]
    public async Task Controleur_ChangementDeMotDePasse_DeconnecteLesAutresAppareilsSeulement()
    {
        var user = await _identity.CreerUtilisatrice("vol@local");
        var courant = _identity.CreerController();
        var vole = _identity.CreerController();
        await courant.Login(new IdentifiantsDto { Email = "vol@local", Password = IdentityDeTest.MotDePasseValide });
        await vole.Login(new IdentifiantsDto { Email = "vol@local", Password = IdentityDeTest.MotDePasseValide });
        var jetonCourant = JetonPose(courant);

        var changement = _identity.CreerController(user.Id);
        changement.ControllerContext.HttpContext.Request.Headers.Cookie = $"refreshToken={jetonCourant}";
        Assert.IsType<OkResult>(await changement.ChangePassword(new ChangementMotDePasseDto
        {
            CurrentPassword = IdentityDeTest.MotDePasseValide,
            NewPassword = "NouveauMotDePasse2!",
        }));

        var apresCourant = _identity.CreerController();
        apresCourant.ControllerContext.HttpContext.Request.Headers.Cookie = $"refreshToken={jetonCourant}";
        Assert.IsType<OkObjectResult>(await apresCourant.RefreshToken());
        var apresVole = _identity.CreerController();
        apresVole.ControllerContext.HttpContext.Request.Headers.Cookie = $"refreshToken={JetonPose(vole)}";
        Assert.IsType<BadRequestObjectResult>(await apresVole.RefreshToken());
    }

    /// <summary>Valeur du cookie de renouvellement posé par le contrôleur.</summary>
    internal static string JetonPose(ControllerBase controller)
    {
        var cookie = controller.Response.Headers.SetCookie.ToString().Split(',').Select(c => c.Trim()).Last(c => c.StartsWith("refreshToken="));
        return cookie["refreshToken=".Length..].Split(';')[0];
    }

    public void Dispose() => _identity.Dispose();
}

public sealed class HorlogeMobile(DateTimeOffset depart) : TimeProvider
{
    private DateTimeOffset _maintenant = depart;
    public override DateTimeOffset GetUtcNow() => _maintenant;
    public void Avancer(TimeSpan duree) => _maintenant += duree;
}
