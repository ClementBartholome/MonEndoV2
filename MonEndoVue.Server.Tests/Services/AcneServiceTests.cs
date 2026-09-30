using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Cycle;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>Suivi de l'acné de la session : épisodes (en cours, terminés, sans chevauchement) et suivis photo.</summary>
public sealed class AcneServiceTests : IDisposable
{
    private static readonly DateOnly Jour = new(2026, 9, 15);
    private static readonly DateTimeOffset Maintenant = new(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly CarnetDeTest _carnet = new();

    private readonly FauxStockagePhotos _stockage = new();

    private AcneService Service() => new(_carnet.Context, _carnet.CarnetSanteService, new HorlogeFixe(Maintenant), _stockage);

    private async Task<int> Creer(DateOnly debut, DateOnly? fin = null, string userId = CarnetDeTest.UserId)
    {
        var resultat = await Service().CreerAsync(userId, new EpisodeAcneDto { Debut = debut, Fin = fin }, CancellationToken.None);
        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        return resultat.Valeur;
    }

    [Fact]
    public async Task Get_EpisodesDuPlusRecentEtSuivisAvecPhoto_DuSeulCarnet()
    {
        await Creer(new DateOnly(2026, 6, 3), new DateOnly(2026, 7, 20));
        await Creer(new DateOnly(2026, 8, 2));
        _carnet.Context.EpisodesAcne.Add(new EpisodeAcne { CarnetSanteId = CarnetDeTest.AutreCarnetSanteId, Debut = new DateOnly(2026, 9, 1) });
        _carnet.Context.SymptomesCycles.AddRange(
            new SymptomeCycle { CarnetSanteId = CarnetDeTest.CarnetSanteId, TypeSymptome = "Acné", Date = new DateTime(2026, 9, 13, 20, 5, 0), Intensite = 4, PhotoUrl = "https://stockage.test/a.jpg" },
            // Un ancien jour d'acné sans photo n'est pas un point de suivi.
            new SymptomeCycle { CarnetSanteId = CarnetDeTest.CarnetSanteId, TypeSymptome = "Acné", Date = new DateTime(2026, 9, 14), Intensite = 4 },
            new SymptomeCycle { CarnetSanteId = CarnetDeTest.CarnetSanteId, TypeSymptome = "Fatigue", Date = new DateTime(2026, 9, 14), Intensite = 4, PhotoUrl = "https://stockage.test/b.jpg" });
        await _carnet.Context.SaveChangesAsync();

        var vue = (await Service().GetAsync(CarnetDeTest.UserId, Jour, CancellationToken.None)).Valeur!;

        Assert.Equal(["2026-08-02", "2026-06-03"], vue.Episodes.Select(e => e.Debut));
        Assert.Null(vue.Episodes[0].Fin);
        Assert.Equal(45, vue.Episodes[0].Jours);
        Assert.Equal("2026-07-20", vue.Episodes[1].Fin);
        Assert.Equal(48, vue.Episodes[1].Jours);
        var suivi = Assert.Single(vue.Suivis);
        Assert.Equal("2026-09-13T20:05:00", suivi.Date);
    }

    [Fact]
    public async Task Get_PhotosDesSeptDerniersMois_LesAutresSurDemande()
    {
        _carnet.Context.SymptomesCycles.AddRange(Enumerable.Range(0, 60).Select(i => new SymptomeCycle
        {
            CarnetSanteId = CarnetDeTest.CarnetSanteId, TypeSymptome = "Acné", Intensite = 3,
            Date = new DateTime(2026, 9, 13).AddDays(-7 * i), PhotoUrl = $"https://stockage.test/{i}.jpg",
        }));
        await _carnet.Context.SaveChangesAsync();

        var parDefaut = (await Service().GetAsync(CarnetDeTest.UserId, Jour, CancellationToken.None)).Valeur!;
        var tout = (await Service().GetAsync(CarnetDeTest.UserId, Jour, CancellationToken.None, 24)).Valeur!;

        // Du 15 février au 13 septembre 2026 : 31 photos hebdomadaires.
        Assert.Equal(31, parDefaut.Suivis.Count);
        Assert.Equal(29, parDefaut.SuivisPlusAnciens);
        Assert.Equal(60, tout.Suivis.Count);
        Assert.Equal(0, tout.SuivisPlusAnciens);
    }

    [Fact]
    public async Task Terminer_PuisNouvelEpisode()
    {
        var id = await Creer(new DateOnly(2026, 8, 2));

        Assert.Equal(StatutOperation.Succes, (await Service().TerminerAsync(CarnetDeTest.UserId, id, new DateOnly(2026, 9, 10), CancellationToken.None)).Statut);
        await Creer(new DateOnly(2026, 9, 14));

        var episodes = await _carnet.Context.EpisodesAcne.OrderBy(e => e.Debut).ToListAsync();
        Assert.Equal(new DateOnly(2026, 9, 10), episodes[0].Fin);
        Assert.Null(episodes[1].Fin);
    }

    [Fact]
    public async Task Saisies_Incoherentes_Refusees()
    {
        var id = await Creer(new DateOnly(2026, 8, 2));
        var service = Service();

        Assert.Equal(StatutOperation.Invalide, (await service.CreerAsync(CarnetDeTest.UserId, new EpisodeAcneDto { Debut = new DateOnly(2026, 9, 14) }, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Invalide, (await service.CreerAsync(CarnetDeTest.UserId, new EpisodeAcneDto { Debut = new DateOnly(2026, 7, 1), Fin = new DateOnly(2026, 8, 5) }, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Invalide, (await service.TerminerAsync(CarnetDeTest.UserId, id, new DateOnly(2026, 8, 1), CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Invalide, (await service.TerminerAsync(CarnetDeTest.UserId, id, Jour.AddDays(5), CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Invalide, (await service.CreerAsync(CarnetDeTest.UserId, new EpisodeAcneDto { Debut = Jour.AddDays(3) }, CancellationToken.None)).Statut);
    }

    [Fact]
    public async Task Modifier_CorrigeLesDates_SansSeChevaucherLuiMeme()
    {
        var id = await Creer(new DateOnly(2026, 8, 2), new DateOnly(2026, 8, 20));

        var resultat = await Service().ModifierAsync(CarnetDeTest.UserId, id, new EpisodeAcneDto { Debut = new DateOnly(2026, 8, 1), Fin = new DateOnly(2026, 8, 25) }, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        var episode = await _carnet.Context.EpisodesAcne.SingleAsync();
        Assert.Equal(new DateOnly(2026, 8, 1), episode.Debut);
        Assert.Equal(new DateOnly(2026, 8, 25), episode.Fin);
    }

    [Fact]
    public async Task EpisodeDUnAutreCarnet_Interdit()
    {
        var autre = new EpisodeAcne { CarnetSanteId = CarnetDeTest.AutreCarnetSanteId, Debut = new DateOnly(2026, 9, 1) };
        _carnet.Context.EpisodesAcne.Add(autre);
        await _carnet.Context.SaveChangesAsync();
        var service = Service();

        Assert.Equal(StatutOperation.Interdit, (await service.TerminerAsync(CarnetDeTest.UserId, autre.Id, Jour, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Interdit, (await service.ModifierAsync(CarnetDeTest.UserId, autre.Id, new EpisodeAcneDto { Debut = Jour }, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Interdit, (await service.SupprimerAsync(CarnetDeTest.UserId, autre.Id, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Introuvable, (await service.SupprimerAsync(CarnetDeTest.UserId, 999, CancellationToken.None)).Statut);
        Assert.Single(await _carnet.Context.EpisodesAcne.ToListAsync());
    }

    [Fact]
    public async Task SansCarnet_NonAuthentifie()
    {
        Assert.Equal(StatutOperation.NonAuthentifie, (await Service().GetAsync("inconnue", Jour, CancellationToken.None)).Statut);
    }

    private async Task<int> SuiviAvecPhoto(int carnet, string url)
    {
        var suivi = new SymptomeCycle { CarnetSanteId = carnet, TypeSymptome = "Acné", Date = new DateTime(2026, 9, 13), Intensite = 3, PhotoUrl = url };
        _carnet.Context.SymptomesCycles.Add(suivi);
        await _carnet.Context.SaveChangesAsync();
        return suivi.Id;
    }

    [Fact]
    public async Task Get_LesPhotosPassentParLAPI_JamaisParLAdresseDuStockage()
    {
        var id = await SuiviAvecPhoto(CarnetDeTest.CarnetSanteId, "https://stockage.test/symptomes/1/a.png");

        var vue = (await Service().GetAsync(CarnetDeTest.UserId, Jour, CancellationToken.None)).Valeur!;

        Assert.Equal($"Acne/photos/{id}", vue.Suivis.Single().PhotoUrl);
    }

    [Fact]
    public async Task Photo_DeLaProprietaire_EstLueParLeServeurAvecUnTypeDeduitDeLExtension()
    {
        var url = "https://stockage.test/symptomes/1/a.png";
        _stockage.Contenus[url] = [1, 2, 3];
        var id = await SuiviAvecPhoto(CarnetDeTest.CarnetSanteId, url);

        var resultat = await Service().PhotoAsync(CarnetDeTest.UserId, id, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        Assert.Equal("image/png", resultat.Valeur!.TypeContenu);
        using var flux = new MemoryStream();
        await resultat.Valeur.Contenu.CopyToAsync(flux);
        Assert.Equal([1, 2, 3], flux.ToArray());
    }

    [Fact]
    public async Task Photo_DUnAutreCarnet_EstRefusee()
    {
        var url = "https://stockage.test/symptomes/2/b.jpg";
        _stockage.Contenus[url] = [9];
        var id = await SuiviAvecPhoto(CarnetDeTest.AutreCarnetSanteId, url);

        var resultat = await Service().PhotoAsync(CarnetDeTest.UserId, id, CancellationToken.None);

        Assert.Equal(StatutOperation.Interdit, resultat.Statut);
    }

    [Fact]
    public async Task Photo_InexistanteOuAbsenteDuStockage_EstIntrouvable()
    {
        var sansPhoto = await SuiviAvecPhoto(CarnetDeTest.CarnetSanteId, "");
        var absenteDuStockage = await SuiviAvecPhoto(CarnetDeTest.CarnetSanteId, "https://stockage.test/symptomes/1/perdue.jpg");

        Assert.Equal(StatutOperation.Introuvable, (await Service().PhotoAsync(CarnetDeTest.UserId, 9999, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Introuvable, (await Service().PhotoAsync(CarnetDeTest.UserId, sansPhoto, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Introuvable, (await Service().PhotoAsync(CarnetDeTest.UserId, absenteDuStockage, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.NonAuthentifie, (await Service().PhotoAsync("", sansPhoto, CancellationToken.None)).Statut);
    }

    public void Dispose() => _carnet.Dispose();
}
