using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services.Agenda;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.SuppressionCompte;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>Suppression définitive du compte : il ne doit rien rester, ni en base ni dans le stockage des photos.</summary>
public sealed class SuppressionCompteServiceTests : IDisposable
{
    private const string Photo = "https://stockage.test/photos/symptomes/1/a.jpg";
    private const string PhotoAutre = "https://stockage.test/photos/symptomes/2/b.jpg";

    private readonly IdentityDeTest _identity = new();
    private readonly FauxStockagePhotos _photos = new();
    private readonly FauxGoogleCalendar _google = LiaisonAgendaDeTest.FauxGoogle();

    [Fact]
    public async Task SupprimerAsync_BonMotDePasse_NeLaisseRienDuCompte()
    {
        var (user, carnetId) = await CreerCompteRempli("a-supprimer@local", Photo);

        var resultat = await Service().SupprimerAsync(user.Id, IdentityDeTest.MotDePasseValide, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        var ctx = _identity.Context;
        Assert.Null(await _identity.UserManager.FindByIdAsync(user.Id));
        Assert.False(await ctx.CarnetSantes.AnyAsync(c => c.Id == carnetId));
        Assert.False(await ctx.DonneesDouleurs.AnyAsync(e => e.CarnetSanteId == carnetId));
        Assert.False(await ctx.DonneesActivitePhysique.AnyAsync(e => e.CarnetSanteId == carnetId));
        Assert.False(await ctx.DonneesTransit.AnyAsync(e => e.CarnetSanteId == carnetId));
        Assert.False(await ctx.JourRegles.AnyAsync(e => e.CarnetSanteId == carnetId));
        Assert.False(await ctx.BilansQuotidiens.AnyAsync(e => e.CarnetSanteId == carnetId));
        Assert.False(await ctx.SymptomesCycles.AnyAsync(e => e.CarnetSanteId == carnetId));
        Assert.False(await ctx.EpisodesAcne.AnyAsync(e => e.CarnetSanteId == carnetId));
        Assert.False(await ctx.Medicaments.AnyAsync(e => e.CarnetSanteId == carnetId));
        Assert.False(await ctx.DonneesMedicaments.AnyAsync(e => e.CarnetSanteId == carnetId));
        Assert.False(await ctx.DonneesTraitementNonMedicamenteux.AnyAsync(e => e.CarnetSanteId == carnetId));
        Assert.False(await ctx.Rappels.AnyAsync(e => e.CarnetSanteId == carnetId));
        Assert.False(await ctx.AbonnementsPush.AnyAsync(e => e.CarnetSanteId == carnetId));
        Assert.False(await ctx.LiaisonsAgenda.AnyAsync(e => e.CarnetSanteId == carnetId));
        Assert.DoesNotContain(Photo, _photos.Contenus.Keys);
    }

    [Fact]
    public async Task SupprimerAsync_NeTouchepasAuxDonneesDUneAutreUtilisatrice()
    {
        var (user, _) = await CreerCompteRempli("a-supprimer@local", Photo);
        var (autre, autreCarnetId) = await CreerCompteRempli("autre@local", PhotoAutre);

        await Service().SupprimerAsync(user.Id, IdentityDeTest.MotDePasseValide, CancellationToken.None);

        Assert.NotNull(await _identity.UserManager.FindByIdAsync(autre.Id));
        Assert.True(await _identity.Context.DonneesDouleurs.AnyAsync(e => e.CarnetSanteId == autreCarnetId));
        Assert.True(await _identity.Context.DonneesMedicaments.AnyAsync(e => e.CarnetSanteId == autreCarnetId));
        Assert.Contains(PhotoAutre, _photos.Contenus.Keys);
    }

    [Fact]
    public async Task SupprimerAsync_MauvaisMotDePasse_NeSupprimeRien()
    {
        var (user, carnetId) = await CreerCompteRempli("a-supprimer@local", Photo);

        var resultat = await Service().SupprimerAsync(user.Id, IdentityDeTest.MotDePasseValide + "-erreur", CancellationToken.None);

        Assert.Equal(StatutOperation.Invalide, resultat.Statut);
        Assert.True(await _identity.Context.CarnetSantes.AnyAsync(c => c.Id == carnetId));
        Assert.Contains(Photo, _photos.Contenus.Keys);
    }

    [Fact]
    public async Task SupprimerAsync_StockageIndisponible_NeSupprimeRienEnBase()
    {
        var (user, carnetId) = await CreerCompteRempli("a-supprimer@local", Photo);
        _photos.Indisponible = true;

        var resultat = await Service().SupprimerAsync(user.Id, IdentityDeTest.MotDePasseValide, CancellationToken.None);

        Assert.Equal(StatutOperation.Indisponible, resultat.Statut);
        Assert.NotNull(await _identity.UserManager.FindByIdAsync(user.Id));
        Assert.True(await _identity.Context.DonneesDouleurs.AnyAsync(e => e.CarnetSanteId == carnetId));
    }

    [Fact]
    public async Task SupprimerAsync_UtilisatriceInconnue_NonAuthentifiee()
    {
        var resultat = await Service().SupprimerAsync("inconnue", IdentityDeTest.MotDePasseValide, CancellationToken.None);

        Assert.Equal(StatutOperation.NonAuthentifie, resultat.Statut);
    }

    [Fact]
    public async Task SupprimerDefinitivementAsync_CompteSansCarnet_SupprimeLeCompte()
    {
        var user = await _identity.CreerUtilisatrice("sans-carnet@local", avecCarnet: false);

        var resultat = await Service().SupprimerDefinitivementAsync(user, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        Assert.Null(await _identity.UserManager.FindByIdAsync(user.Id));
    }

    [Fact]
    public async Task SupprimerCompte_Succes_FermeLaSession()
    {
        var (user, _) = await CreerCompteRempli("a-supprimer@local", Photo);
        var controller = new SuppressionCompteController(Service())
        {
            ControllerContext = _identity.CreerController(user.Id).ControllerContext,
        };

        var resultat = await controller.SupprimerCompte(new SuppressionCompteDto { Password = IdentityDeTest.MotDePasseValide }, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultat);
        Assert.Contains(controller.Response.Headers.SetCookie, c => c!.StartsWith("accessToken=;"));
    }

    [Fact]
    public async Task SupprimerCompte_MauvaisMotDePasse_RetourneBadRequest()
    {
        var (user, _) = await CreerCompteRempli("a-supprimer@local", Photo);
        var controller = new SuppressionCompteController(Service())
        {
            ControllerContext = _identity.CreerController(user.Id).ControllerContext,
        };

        var resultat = await controller.SupprimerCompte(new SuppressionCompteDto { Password = IdentityDeTest.MotDePasseValide + "-erreur" }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(resultat);
    }

    [Fact]
    public async Task SupprimerAsync_AgendaLie_RevoqueLAccordChezGoogle()
    {
        var user = await _identity.CreerUtilisatrice("agenda@local");
        var protection = LiaisonAgendaDeTest.Protection();
        var liaison = LiaisonAgendaDeTest.Service(_identity.Context, _google, protection: protection);
        var debut = await liaison.DemarrerAsync(user.Id, CancellationToken.None);
        var etat = Uri.UnescapeDataString(debut.Valeur!.UrlAutorisation.Split("state=")[1].Split('&')[0]);
        await liaison.FinaliserAsync(debut.Valeur.CookieEtat, etat, LiaisonAgendaDeTest.Code, null, CancellationToken.None);
        _google.Corps.Clear();
        var service = new SuppressionCompteService(
            _identity.Context, _identity.UserManager, _photos,
            LiaisonAgendaDeTest.Service(_identity.Context, _google, protection: protection),
            NullLogger<SuppressionCompteService>.Instance);

        var resultat = await service.SupprimerAsync(user.Id, IdentityDeTest.MotDePasseValide, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        Assert.Equal(GoogleOAuthClient.UrlRevocation, Assert.Single(_google.Requetes.TakeLast(1)).RequestUri!.ToString());
        Assert.Contains(LiaisonAgendaDeTest.JetonActualisation, Assert.Single(_google.Corps));
        Assert.False(await _identity.Context.LiaisonsAgenda.AnyAsync());
    }

    private SuppressionCompteService Service() =>
        new(_identity.Context, _identity.UserManager, _photos, LiaisonAgendaDeTest.Service(_identity.Context, _google), NullLogger<SuppressionCompteService>.Instance);

    /// <summary>Compte avec une donnée de chaque sorte, dont une photo de suivi.</summary>
    private async Task<(ApplicationUser User, int CarnetId)> CreerCompteRempli(string email, string photo)
    {
        var user = await _identity.CreerUtilisatrice(email);
        var ctx = _identity.Context;
        var carnetId = await ctx.CarnetSantes.Where(c => c.UserId == user.Id).Select(c => c.Id).SingleAsync();
        var jour = new DateTime(2026, 9, 1);

        var traitement = new Medicament { CarnetSanteId = carnetId, Nom = "Traitement", Type = TypeTraitement.Medicamenteux };
        ctx.Medicaments.Add(traitement);
        ctx.DonneesDouleurs.Add(new DonneesDouleur { CarnetSanteId = carnetId, TypeDouleur = "Pelvienne", Intensite = 5, Date = jour });
        ctx.DonneesActivitePhysique.Add(new DonneesActivitePhysique { CarnetSanteId = carnetId, TypeActivite = "Marche", Date = jour });
        ctx.DonneesTransit.Add(new DonneesTransit { CarnetSanteId = carnetId, TypeEvenement = "Constipation", Intensite = "Légère", Date = jour });
        ctx.JourRegles.Add(new JourRegle { CarnetSanteId = carnetId, Date = jour });
        ctx.BilansQuotidiens.Add(new BilanQuotidien { CarnetSanteId = carnetId, Date = jour, Emotions = [new EmotionBilan { Emotion = Emotion.Calme }] });
        ctx.SymptomesCycles.Add(new SymptomeCycle { CarnetSanteId = carnetId, TypeSymptome = "Acné", Date = jour, PhotoUrl = photo });
        ctx.EpisodesAcne.Add(new EpisodeAcne { CarnetSanteId = carnetId, Debut = DateOnly.FromDateTime(jour) });
        ctx.Rappels.Add(new Rappel { CarnetSanteId = carnetId, Type = TypeRappel.BilanQuotidien });
        ctx.AbonnementsPush.Add(new AbonnementPush { CarnetSanteId = carnetId, Endpoint = $"https://push.test/{email}" });
        ctx.LiaisonsAgenda.Add(new LiaisonAgenda { CarnetSanteId = carnetId, JetonActualisationProtege = "illisible" });
        await ctx.SaveChangesAsync();
        ctx.DonneesMedicaments.Add(new DonneesMedicament { CarnetSanteId = carnetId, MedicamentId = traitement.Id, Date = jour });
        ctx.DonneesTraitementNonMedicamenteux.Add(new DonneesTraitementNonMedicamenteux { CarnetSanteId = carnetId, MedicamentId = traitement.Id, Date = jour });
        await ctx.SaveChangesAsync();
        _photos.Contenus[photo] = [1];

        return (user, carnetId);
    }

    public void Dispose() => _identity.Dispose();
}
