using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Agenda;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

public sealed class LiaisonAgendaServiceTests : IDisposable
{
    private const string UserId = CarnetDeTest.UserId;

    private readonly CarnetDeTest _carnet = new();
    private readonly IDataProtectionProvider _protection = LiaisonAgendaDeTest.Protection();

    public void Dispose() => _carnet.Dispose();

    private LiaisonAgendaService Service(
        FauxGoogleCalendar google, bool configuree = true, DateTimeOffset? maintenant = null, IMemoryCache? cache = null) =>
        LiaisonAgendaDeTest.Service(
            _carnet.Context, google, LiaisonAgendaDeTest.Options(configuree), maintenant, _protection, cache);

    private static string Parametre(string url, string nom) =>
        Uri.UnescapeDataString(url.Split($"{nom}=")[1].Split('&')[0]);

    /// <summary>Départ puis retour de Google avec le bon état : le parcours nominal d'une liaison.</summary>
    private async Task<ResultatOperation> Lier(LiaisonAgendaService service, string userId = UserId)
    {
        var debut = (await service.DemarrerAsync(userId, CancellationToken.None)).Valeur!;
        return await service.FinaliserAsync(
            debut.CookieEtat, Parametre(debut.UrlAutorisation, "state"), LiaisonAgendaDeTest.Code, null, CancellationToken.None);
    }

    [Fact]
    public async Task DemarrerAsync_NonConfiguree_IndisponibleSansCookie()
    {
        var resultat = await Service(LiaisonAgendaDeTest.FauxGoogle(), configuree: false).DemarrerAsync(UserId, CancellationToken.None);

        Assert.Equal(StatutOperation.Indisponible, resultat.Statut);
        Assert.Null(resultat.Valeur);
    }

    [Fact]
    public async Task DemarrerAsync_UtilisatriceSansCarnet_NonAuthentifiee()
    {
        var resultat = await Service(LiaisonAgendaDeTest.FauxGoogle()).DemarrerAsync("inconnue", CancellationToken.None);

        Assert.Equal(StatutOperation.NonAuthentifie, resultat.Statut);
    }

    [Fact]
    public async Task DemarrerAsync_Configuree_UrlGoogleAvecPkceEtEtatLieAuCookie()
    {
        var service = Service(LiaisonAgendaDeTest.FauxGoogle());

        var debut = (await service.DemarrerAsync(UserId, CancellationToken.None)).Valeur!;

        Assert.StartsWith(GoogleOAuthClient.UrlAutorisation + "?", debut.UrlAutorisation);
        Assert.Equal("client-de-test", Parametre(debut.UrlAutorisation, "client_id"));
        Assert.Equal("https://monendo.test/Agenda/liaison/callback", Parametre(debut.UrlAutorisation, "redirect_uri"));
        Assert.Equal(LiaisonAgendaDeTest.PorteesAccordees, Parametre(debut.UrlAutorisation, "scope"));
        Assert.Equal("code", Parametre(debut.UrlAutorisation, "response_type"));
        Assert.Equal("offline", Parametre(debut.UrlAutorisation, "access_type"));
        Assert.Equal("S256", Parametre(debut.UrlAutorisation, "code_challenge_method"));
        Assert.DoesNotContain("secret-de-test", debut.UrlAutorisation);

        var etat = EtatLiaison.Lire(
            _protection.CreateProtector("LiaisonAgenda.Etat"), debut.CookieEtat, Parametre(debut.UrlAutorisation, "state"), AgendaDeTest.Maintenant);
        Assert.NotNull(etat);
        Assert.Equal(UserId, etat.UserId);
        var defi = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(etat.VerificateurPkce)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Assert.Equal(defi, Parametre(debut.UrlAutorisation, "code_challenge"));
    }

    [Fact]
    public async Task DemarrerAsync_DeuxFois_EtatsDifferents()
    {
        var service = Service(LiaisonAgendaDeTest.FauxGoogle());

        var premier = (await service.DemarrerAsync(UserId, CancellationToken.None)).Valeur!;
        var second = (await service.DemarrerAsync(UserId, CancellationToken.None)).Valeur!;

        Assert.NotEqual(Parametre(premier.UrlAutorisation, "state"), Parametre(second.UrlAutorisation, "state"));
    }

    [Fact]
    public async Task FinaliserAsync_RetourValide_EnregistreLeJetonChiffreEtEchangeLeCodeAvecLeVerificateur()
    {
        var google = LiaisonAgendaDeTest.FauxGoogle();
        var service = Service(google);
        var debut = (await service.DemarrerAsync(UserId, CancellationToken.None)).Valeur!;
        var etat = Parametre(debut.UrlAutorisation, "state");

        var resultat = await service.FinaliserAsync(debut.CookieEtat, etat, LiaisonAgendaDeTest.Code, null, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        var liaison = await _carnet.Context.LiaisonsAgenda.SingleAsync();
        Assert.Equal(CarnetDeTest.CarnetSanteId, liaison.CarnetSanteId);
        Assert.Equal(AgendaDeTest.Maintenant.UtcDateTime, liaison.LieeLe);
        Assert.DoesNotContain(LiaisonAgendaDeTest.JetonActualisation, liaison.JetonActualisationProtege);
        Assert.Equal(
            LiaisonAgendaDeTest.JetonActualisation,
            _protection.CreateProtector("LiaisonAgenda.JetonActualisation").Unprotect(liaison.JetonActualisationProtege));

        Assert.Equal(GoogleOAuthClient.UrlJeton, Assert.Single(google.Requetes).RequestUri!.ToString());
        var corps = Assert.Single(google.Corps);
        Assert.Contains("grant_type=authorization_code", corps);
        Assert.Contains($"code={LiaisonAgendaDeTest.Code}", corps);
        Assert.Contains("code_verifier=", corps);
        Assert.Contains("client_secret=secret-de-test", corps);
    }

    [Fact]
    public async Task FinaliserAsync_RetourValide_LeJetonDAccesEstDejaEnCache()
    {
        var google = LiaisonAgendaDeTest.FauxGoogle();
        var service = Service(google);
        await Lier(service);

        var jeton = await service.JetonAccesAsync(UserId, CancellationToken.None);

        Assert.Equal(LiaisonAgendaDeTest.JetonAcces, jeton.Valeur);
        Assert.Single(google.Requetes);
    }

    [Fact]
    public async Task FinaliserAsync_EtatDifferentDeCeluiDuCookie_RefuseSansAppelerGoogle()
    {
        var google = LiaisonAgendaDeTest.FauxGoogle();
        var service = Service(google);
        var debut = (await service.DemarrerAsync(UserId, CancellationToken.None)).Valeur!;

        var resultat = await service.FinaliserAsync(debut.CookieEtat, "etat-forge", LiaisonAgendaDeTest.Code, null, CancellationToken.None);

        Assert.Equal(StatutOperation.Invalide, resultat.Statut);
        Assert.Empty(google.Requetes);
        Assert.False(await _carnet.Context.LiaisonsAgenda.AnyAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("cookie-altere")]
    public async Task FinaliserAsync_CookieAbsentOuAltere_RefuseSansAppelerGoogle(string? cookie)
    {
        var google = LiaisonAgendaDeTest.FauxGoogle();
        var service = Service(google);
        var debut = (await service.DemarrerAsync(UserId, CancellationToken.None)).Valeur!;

        var resultat = await service.FinaliserAsync(
            cookie, Parametre(debut.UrlAutorisation, "state"), LiaisonAgendaDeTest.Code, null, CancellationToken.None);

        Assert.Equal(StatutOperation.Invalide, resultat.Statut);
        Assert.Empty(google.Requetes);
    }

    [Fact]
    public async Task FinaliserAsync_CookieDUneAutreInstallation_Refuse()
    {
        var google = LiaisonAgendaDeTest.FauxGoogle();
        var debut = (await Service(google).DemarrerAsync(UserId, CancellationToken.None)).Valeur!;
        var autreCle = LiaisonAgendaDeTest.Service(
            _carnet.Context, google, protection: LiaisonAgendaDeTest.Protection());

        var resultat = await autreCle.FinaliserAsync(
            debut.CookieEtat, Parametre(debut.UrlAutorisation, "state"), LiaisonAgendaDeTest.Code, null, CancellationToken.None);

        Assert.Equal(StatutOperation.Invalide, resultat.Statut);
        Assert.Empty(google.Requetes);
    }

    [Fact]
    public async Task FinaliserAsync_EtatExpire_RefuseSansAppelerGoogle()
    {
        var google = LiaisonAgendaDeTest.FauxGoogle();
        var debut = (await Service(google).DemarrerAsync(UserId, CancellationToken.None)).Valeur!;
        var plusTard = Service(google, maintenant: AgendaDeTest.Maintenant + EtatLiaison.Duree + TimeSpan.FromSeconds(1));

        var resultat = await plusTard.FinaliserAsync(
            debut.CookieEtat, Parametre(debut.UrlAutorisation, "state"), LiaisonAgendaDeTest.Code, null, CancellationToken.None);

        Assert.Equal(StatutOperation.Invalide, resultat.Statut);
        Assert.Empty(google.Requetes);
    }

    [Theory]
    [InlineData("access_denied", LiaisonAgendaDeTest.Code)]
    [InlineData(null, null)]
    [InlineData(null, "")]
    public async Task FinaliserAsync_RefusOuSansCode_RienEnregistre(string? erreur, string? code)
    {
        var google = LiaisonAgendaDeTest.FauxGoogle();
        var service = Service(google);
        var debut = (await service.DemarrerAsync(UserId, CancellationToken.None)).Valeur!;

        var resultat = await service.FinaliserAsync(
            debut.CookieEtat, Parametre(debut.UrlAutorisation, "state"), code, erreur, CancellationToken.None);

        Assert.Equal(StatutOperation.Invalide, resultat.Statut);
        Assert.Empty(google.Requetes);
        Assert.False(await _carnet.Context.LiaisonsAgenda.AnyAsync());
    }

    [Fact]
    public async Task FinaliserAsync_CarnetSupprimeEntreTemps_NonAuthentifiee()
    {
        var google = LiaisonAgendaDeTest.FauxGoogle();
        var service = Service(google);
        var debut = (await service.DemarrerAsync(UserId, CancellationToken.None)).Valeur!;
        _carnet.Context.CarnetSantes.Remove(await _carnet.Context.CarnetSantes.SingleAsync(c => c.UserId == UserId));
        await _carnet.Context.SaveChangesAsync();

        var resultat = await service.FinaliserAsync(
            debut.CookieEtat, Parametre(debut.UrlAutorisation, "state"), LiaisonAgendaDeTest.Code, null, CancellationToken.None);

        Assert.Equal(StatutOperation.NonAuthentifie, resultat.Statut);
        Assert.Empty(google.Requetes);
    }

    [Fact]
    public async Task FinaliserAsync_EchecDeLEchange_Indisponible()
    {
        var service = Service(LiaisonAgendaDeTest.FauxGoogle(_ => LiaisonAgendaDeTest.Erreur("invalid_grant")));

        var resultat = await Lier(service);

        Assert.Equal(StatutOperation.Indisponible, resultat.Statut);
        Assert.False(await _carnet.Context.LiaisonsAgenda.AnyAsync());
    }

    [Fact]
    public async Task FinaliserAsync_GoogleNeRepondPas_Indisponible()
    {
        var service = Service(LiaisonAgendaDeTest.FauxGoogle(_ => throw new HttpRequestException("réseau")));

        var resultat = await Lier(service);

        Assert.Equal(StatutOperation.Indisponible, resultat.Statut);
        Assert.False(await _carnet.Context.LiaisonsAgenda.AnyAsync());
    }

    [Fact]
    public async Task FinaliserAsync_SansJetonDActualisation_Indisponible()
    {
        var service = Service(LiaisonAgendaDeTest.FauxGoogle(_ => LiaisonAgendaDeTest.Jetons(actualisation: null)));

        var resultat = await Lier(service);

        Assert.Equal(StatutOperation.Indisponible, resultat.Statut);
        Assert.False(await _carnet.Context.LiaisonsAgenda.AnyAsync());
    }

    [Fact]
    public async Task FinaliserAsync_ReponseSansJetonDAcces_Indisponible()
    {
        var service = Service(LiaisonAgendaDeTest.FauxGoogle(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        }));

        var resultat = await Lier(service);

        Assert.Equal(StatutOperation.Indisponible, resultat.Statut);
    }

    [Fact]
    public async Task FinaliserAsync_DejaLiee_RemplaceLeJetonEtRevoqueLAncien()
    {
        var calls = 0;
        var google = LiaisonAgendaDeTest.FauxGoogle(_ =>
            ++calls == 1 ? LiaisonAgendaDeTest.Jetons(actualisation: "ancien") : LiaisonAgendaDeTest.Jetons(actualisation: "nouveau"));
        var service = Service(google);
        await Lier(service);

        await Lier(service);

        var liaison = await _carnet.Context.LiaisonsAgenda.SingleAsync();
        Assert.Equal("nouveau", _protection.CreateProtector("LiaisonAgenda.JetonActualisation").Unprotect(liaison.JetonActualisationProtege));
        Assert.Equal(GoogleOAuthClient.UrlRevocation, google.Requetes[^1].RequestUri!.ToString());
        Assert.Equal("token=ancien", google.Corps[^1]);
    }

    [Fact]
    public async Task FinaliserAsync_UnePorteeDecocheeChezGoogle_RefuseRevoqueEtNEnregistreRien()
    {
        var google = LiaisonAgendaDeTest.FauxGoogle(_ =>
            LiaisonAgendaDeTest.Jetons(portee: GoogleOAuthClient.PorteeEvenements));
        var service = Service(google);

        var resultat = await Lier(service);

        Assert.Equal(StatutOperation.Invalide, resultat.Statut);
        Assert.Empty(_carnet.Context.LiaisonsAgenda);
        Assert.Equal(GoogleOAuthClient.UrlRevocation, google.Requetes[^1].RequestUri!.ToString());
        Assert.Equal($"token={LiaisonAgendaDeTest.JetonActualisation}", google.Corps[^1]);
    }

    [Fact]
    public async Task FinaliserAsync_ReponseSansPortees_Refuse()
    {
        var service = Service(LiaisonAgendaDeTest.FauxGoogle(_ => LiaisonAgendaDeTest.Jetons(portee: "")));

        var resultat = await Lier(service);

        Assert.Equal(StatutOperation.Invalide, resultat.Statut);
        Assert.Empty(_carnet.Context.LiaisonsAgenda);
    }

    [Fact]
    public async Task FinaliserAsync_NouvelleLiaison_OublieLeCalendrierChoisi()
    {
        var service = Service(LiaisonAgendaDeTest.FauxGoogle());
        await Lier(service);
        await service.EnregistrerCalendrierAsync(UserId, "ancien@example.com", CancellationToken.None);

        await Lier(service);

        Assert.Null(await service.CalendrierChoisiAsync(UserId, CancellationToken.None));
    }

    [Fact]
    public async Task EnregistrerCalendrierAsync_Liee_EnregistreLeChoixDeCetteUtilisatriceSeulement()
    {
        var service = Service(LiaisonAgendaDeTest.FauxGoogle());
        await Lier(service);
        await Lier(service, CarnetDeTest.AutreUserId);

        var resultat = await service.EnregistrerCalendrierAsync(UserId, "rdv@example.com", CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        Assert.Equal("rdv@example.com", await service.CalendrierChoisiAsync(UserId, CancellationToken.None));
        Assert.Null(await service.CalendrierChoisiAsync(CarnetDeTest.AutreUserId, CancellationToken.None));
        Assert.Equal("rdv@example.com", (await service.StatutAsync(UserId, CancellationToken.None)).CalendrierId);
    }

    [Fact]
    public async Task EnregistrerCalendrierAsync_SansLiaison_IntrouvableEtRienEnregistre()
    {
        var service = Service(LiaisonAgendaDeTest.FauxGoogle());

        var resultat = await service.EnregistrerCalendrierAsync(UserId, "rdv@example.com", CancellationToken.None);

        Assert.Equal(StatutOperation.Introuvable, resultat.Statut);
        Assert.Empty(_carnet.Context.LiaisonsAgenda);
    }

    [Fact]
    public async Task DelierAsync_Liee_SupprimeLaLiaisonEtRevoqueLAccord()
    {
        var google = LiaisonAgendaDeTest.FauxGoogle();
        var service = Service(google);
        await Lier(service);

        var resultat = await service.DelierAsync(UserId, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        Assert.False(await _carnet.Context.LiaisonsAgenda.AnyAsync());
        Assert.Equal(GoogleOAuthClient.UrlRevocation, google.Requetes[^1].RequestUri!.ToString());
        Assert.Equal($"token={LiaisonAgendaDeTest.JetonActualisation}", google.Corps[^1]);
        Assert.Equal(StatutOperation.Introuvable, (await service.JetonAccesAsync(UserId, CancellationToken.None)).Statut);
    }

    [Fact]
    public async Task DelierAsync_RevocationEnEchec_SupprimeQuandMemeLaLiaison()
    {
        var revocation = false;
        var google = LiaisonAgendaDeTest.FauxGoogle(r =>
            r.RequestUri!.ToString() == GoogleOAuthClient.UrlRevocation && (revocation = true)
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : LiaisonAgendaDeTest.Jetons());
        var service = Service(google);
        await Lier(service);

        var resultat = await service.DelierAsync(UserId, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        Assert.True(revocation);
        Assert.False(await _carnet.Context.LiaisonsAgenda.AnyAsync());
    }

    [Fact]
    public async Task DelierAsync_GoogleInjoignable_SupprimeQuandMemeLaLiaison()
    {
        var google = LiaisonAgendaDeTest.FauxGoogle(r =>
            r.RequestUri!.ToString() == GoogleOAuthClient.UrlRevocation ? throw new HttpRequestException("réseau") : LiaisonAgendaDeTest.Jetons());
        var service = Service(google);
        await Lier(service);

        var resultat = await service.DelierAsync(UserId, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        Assert.False(await _carnet.Context.LiaisonsAgenda.AnyAsync());
    }

    [Fact]
    public async Task DelierAsync_SansLiaison_Introuvable()
    {
        var resultat = await Service(LiaisonAgendaDeTest.FauxGoogle()).DelierAsync(UserId, CancellationToken.None);

        Assert.Equal(StatutOperation.Introuvable, resultat.Statut);
    }

    [Fact]
    public async Task DelierAsync_NeTouchePasLaLiaisonDUneAutreUtilisatrice()
    {
        var service = Service(LiaisonAgendaDeTest.FauxGoogle());
        await Lier(service, CarnetDeTest.AutreUserId);

        var resultat = await service.DelierAsync(UserId, CancellationToken.None);

        Assert.Equal(StatutOperation.Introuvable, resultat.Statut);
        Assert.Equal(CarnetDeTest.AutreCarnetSanteId, (await _carnet.Context.LiaisonsAgenda.SingleAsync()).CarnetSanteId);
    }

    [Fact]
    public async Task StatutAsync_RefleteLaLiaisonDeLUtilisatriceSeulement()
    {
        var service = Service(LiaisonAgendaDeTest.FauxGoogle());
        await Lier(service, CarnetDeTest.AutreUserId);

        var sans = await service.StatutAsync(UserId, CancellationToken.None);
        var avec = await service.StatutAsync(CarnetDeTest.AutreUserId, CancellationToken.None);
        var nonConfiguree = await Service(LiaisonAgendaDeTest.FauxGoogle(), configuree: false).StatutAsync(UserId, CancellationToken.None);

        Assert.Equal(new StatutLiaison(true, false, null, null), sans);
        Assert.True(avec.Liee);
        Assert.Equal(AgendaDeTest.Maintenant.UtcDateTime, avec.LieeLe);
        Assert.False(nonConfiguree.Disponible);
    }

    [Fact]
    public async Task JetonAccesAsync_SansLiaisonOuSansConfiguration_IntrouvableSansAppelerGoogle()
    {
        var google = LiaisonAgendaDeTest.FauxGoogle();
        var service = Service(google);
        await Lier(service);
        google.Requetes.Clear();

        var sansLiaison = await service.JetonAccesAsync(CarnetDeTest.AutreUserId, CancellationToken.None);
        var nonConfiguree = await Service(google, configuree: false).JetonAccesAsync(UserId, CancellationToken.None);

        Assert.Equal(StatutOperation.Introuvable, sansLiaison.Statut);
        Assert.Equal(StatutOperation.Introuvable, nonConfiguree.Statut);
        Assert.Empty(google.Requetes);
    }

    [Fact]
    public async Task JetonAccesAsync_CacheVide_ActualiseLeJetonPuisLeMemorise()
    {
        var google = LiaisonAgendaDeTest.FauxGoogle();
        await Lier(Service(google));
        google.Requetes.Clear();
        google.Corps.Clear();
        var service = Service(google); // nouveau cache : le jeton d'accès doit être demandé à Google

        var premier = await service.JetonAccesAsync(UserId, CancellationToken.None);
        var second = await service.JetonAccesAsync(UserId, CancellationToken.None);

        Assert.Equal(LiaisonAgendaDeTest.JetonAcces, premier.Valeur);
        Assert.Equal(LiaisonAgendaDeTest.JetonAcces, second.Valeur);
        Assert.Equal(GoogleOAuthClient.UrlJeton, Assert.Single(google.Requetes).RequestUri!.ToString());
        Assert.Contains("grant_type=refresh_token", Assert.Single(google.Corps));
        Assert.Contains($"refresh_token={LiaisonAgendaDeTest.JetonActualisation}", google.Corps[0]);
    }

    [Fact]
    public async Task JetonAccesAsync_AccordRevoqueChezGoogle_SupprimeLaLiaison()
    {
        var google = LiaisonAgendaDeTest.FauxGoogle();
        await Lier(Service(google));
        var revoque = LiaisonAgendaDeTest.FauxGoogle(_ => LiaisonAgendaDeTest.Erreur("invalid_grant"));

        var resultat = await Service(revoque).JetonAccesAsync(UserId, CancellationToken.None);

        Assert.Equal(StatutOperation.Introuvable, resultat.Statut);
        Assert.False(await _carnet.Context.LiaisonsAgenda.AnyAsync());
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task JetonAccesAsync_GoogleEnErreur_IndisponibleEtLiaisonConservee(HttpStatusCode statut)
    {
        await Lier(Service(LiaisonAgendaDeTest.FauxGoogle()));
        var enPanne = LiaisonAgendaDeTest.FauxGoogle(_ => new HttpResponseMessage(statut));

        var resultat = await Service(enPanne).JetonAccesAsync(UserId, CancellationToken.None);

        Assert.Equal(StatutOperation.Indisponible, resultat.Statut);
        Assert.True(await _carnet.Context.LiaisonsAgenda.AnyAsync());
    }

    [Fact]
    public async Task JetonAccesAsync_ErreurNonJson_IndisponibleEtLiaisonConservee()
    {
        await Lier(Service(LiaisonAgendaDeTest.FauxGoogle()));
        var enPanne = LiaisonAgendaDeTest.FauxGoogle(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("<html>", Encoding.UTF8, "text/html"),
        });

        var resultat = await Service(enPanne).JetonAccesAsync(UserId, CancellationToken.None);

        Assert.Equal(StatutOperation.Indisponible, resultat.Statut);
        Assert.True(await _carnet.Context.LiaisonsAgenda.AnyAsync());
    }

    [Fact]
    public async Task JetonAccesAsync_ClesDeChiffrementPerdues_SupprimeLaLiaisonIllisible()
    {
        var google = LiaisonAgendaDeTest.FauxGoogle();
        await Lier(Service(google));
        var nouvellesCles = LiaisonAgendaDeTest.Service(_carnet.Context, google, protection: LiaisonAgendaDeTest.Protection());
        google.Requetes.Clear();

        var resultat = await nouvellesCles.JetonAccesAsync(UserId, CancellationToken.None);

        Assert.Equal(StatutOperation.Introuvable, resultat.Statut);
        Assert.Empty(google.Requetes);
        Assert.False(await _carnet.Context.LiaisonsAgenda.AnyAsync());
    }

    [Fact]
    public async Task RevoquerPourSuppressionAsync_Liee_RevoqueSansToucherALaBase()
    {
        var google = LiaisonAgendaDeTest.FauxGoogle();
        var service = Service(google);
        await Lier(service);

        await service.RevoquerPourSuppressionAsync(CarnetDeTest.CarnetSanteId, CancellationToken.None);

        Assert.Equal($"token={LiaisonAgendaDeTest.JetonActualisation}", google.Corps[^1]);
        Assert.True(await _carnet.Context.LiaisonsAgenda.AnyAsync());
    }

    [Fact]
    public async Task RevoquerPourSuppressionAsync_SansLiaison_NAppellePasGoogle()
    {
        var google = LiaisonAgendaDeTest.FauxGoogle();

        await Service(google).RevoquerPourSuppressionAsync(CarnetDeTest.CarnetSanteId, CancellationToken.None);

        Assert.Empty(google.Requetes);
    }
}
