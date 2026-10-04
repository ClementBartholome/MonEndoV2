using System.IO.Compression;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Export;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>Export de toutes les données de l'utilisatrice : contenu, photos, cloisonnement.</summary>
public sealed class ExportDonneesServiceTests : IDisposable
{
    private static readonly DateTimeOffset Maintenant = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);

    private readonly CarnetDeTest _carnet = new();
    private readonly FauxStockagePhotos _photos = new();

    [Fact]
    public async Task PreparerAsync_ContientLesDonneesDuCarnetEtSesPhotos()
    {
        var traitement = new Medicament { CarnetSanteId = CarnetDeTest.CarnetSanteId, Nom = "Dienogest", Type = TypeTraitement.Medicamenteux };
        _carnet.Context.Medicaments.Add(traitement);
        _carnet.Context.DonneesDouleurs.Add(new DonneesDouleur { CarnetSanteId = CarnetDeTest.CarnetSanteId, TypeDouleur = "Pelvienne", Intensite = 6, Date = new DateTime(2026, 9, 1) });
        _carnet.Context.SymptomesCycles.Add(new SymptomeCycle
        {
            Id = 10, CarnetSanteId = CarnetDeTest.CarnetSanteId, TypeSymptome = "Acné", Intensite = 2,
            Date = new DateTime(2026, 9, 2), PhotoUrl = "https://stockage.test/photos/symptomes/1/a.png",
        });
        _carnet.Context.BilansQuotidiens.Add(new BilanQuotidien
        {
            CarnetSanteId = CarnetDeTest.CarnetSanteId, Date = new DateTime(2026, 9, 3), DouleurMoyenne = 3,
            Emotions = [new EmotionBilan { Emotion = Emotion.Calme }],
        });
        _carnet.Context.EpisodesAcne.Add(new EpisodeAcne { CarnetSanteId = CarnetDeTest.CarnetSanteId, Debut = new DateOnly(2026, 8, 1), Fin = new DateOnly(2026, 8, 20) });
        _carnet.Context.JourRegles.AddRange(
            new JourRegle { CarnetSanteId = CarnetDeTest.CarnetSanteId, Date = new DateTime(2026, 9, 5), Flux = FluxRegles.Moyen, Caillots = true },
            new JourRegle { CarnetSanteId = CarnetDeTest.CarnetSanteId, Date = new DateTime(2026, 9, 6) });
        await _carnet.Context.SaveChangesAsync();
        _carnet.Context.DonneesMedicaments.Add(new DonneesMedicament { CarnetSanteId = CarnetDeTest.CarnetSanteId, MedicamentId = traitement.Id, NombreComprimes = 1, Date = new DateTime(2026, 9, 4) });
        await _carnet.Context.SaveChangesAsync();
        _photos.Contenus["https://stockage.test/photos/symptomes/1/a.png"] = [1, 2, 3];

        var (archive, nom) = await Preparer();

        Assert.Equal("monendo-mes-donnees-2026-09-27.zip", nom);
        using var zip = new ZipArchive(archive);
        var donnees = LireJson(zip).GetProperty("donnees");
        Assert.Equal(6, donnees.GetProperty("douleurs")[0].GetProperty("intensite").GetInt32());
        Assert.Equal("Dienogest", donnees.GetProperty("prisesDeTraitement")[0].GetProperty("traitement").GetString());
        Assert.Equal("Calme", donnees.GetProperty("bilansQuotidiens")[0].GetProperty("emotions")[0].GetString());
        Assert.Equal("photos/2026-09-02-10.png", donnees.GetProperty("symptomesDuCycle")[0].GetProperty("photo").GetString());
        Assert.Equal(3, zip.GetEntry("photos/2026-09-02-10.png")!.Length);
        Assert.Equal("2026-08-20", donnees.GetProperty("episodesAcne")[0].GetProperty("fin").GetString());
        var regles = donnees.GetProperty("joursDeRegles");
        Assert.Equal("Moyen", regles[0].GetProperty("flux").GetString());
        Assert.True(regles[0].GetProperty("caillots").GetBoolean());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, regles[1].GetProperty("flux").ValueKind);
        Assert.NotNull(zip.GetEntry("LISEZMOI.txt"));
    }

    [Fact]
    public async Task PreparerAsync_NExportePasLesDonneesDUnAutreCarnet()
    {
        _carnet.Context.DonneesDouleurs.Add(new DonneesDouleur { CarnetSanteId = CarnetDeTest.AutreCarnetSanteId, TypeDouleur = "Autre", Intensite = 9, Date = new DateTime(2026, 9, 1) });
        await _carnet.Context.SaveChangesAsync();

        var (archive, _) = await Preparer();

        using var zip = new ZipArchive(archive);
        Assert.Equal(0, LireJson(zip).GetProperty("donnees").GetProperty("douleurs").GetArrayLength());
    }

    [Fact]
    public async Task PreparerAsync_PhotoDisparue_ExporteLeSymptomeSansPhoto()
    {
        _carnet.Context.SymptomesCycles.Add(new SymptomeCycle
        {
            CarnetSanteId = CarnetDeTest.CarnetSanteId, TypeSymptome = "Acné", Date = new DateTime(2026, 9, 2),
            PhotoUrl = "https://stockage.test/photos/disparue.jpg",
        });
        await _carnet.Context.SaveChangesAsync();

        var (archive, _) = await Preparer();

        using var zip = new ZipArchive(archive);
        var symptome = LireJson(zip).GetProperty("donnees").GetProperty("symptomesDuCycle")[0];
        Assert.Equal(JsonValueKind.Null, symptome.GetProperty("photo").ValueKind);
        Assert.DoesNotContain(zip.Entries, e => e.FullName.StartsWith("photos/"));
    }

    [Fact]
    public async Task PreparerAsync_UtilisatriceSansCarnet_RetourneIntrouvable()
    {
        var resultat = await Service().PreparerAsync("inconnue", CancellationToken.None);

        Assert.Equal(StatutOperation.Introuvable, resultat.Statut);
    }

    [Fact]
    public async Task Exporter_RenvoieUneArchiveZip()
    {
        var controller = new DonneesPersonnellesController(Service()) { ControllerContext = CarnetDeTest.ContexteAuthentifie() };

        var resultat = await controller.Exporter(CancellationToken.None);

        var fichier = Assert.IsType<FileStreamResult>(resultat);
        Assert.Equal(ExportDonneesService.TypeContenu, fichier.ContentType);
        await fichier.FileStream.DisposeAsync();
    }

    private ExportDonneesService Service() =>
        new(_carnet.Context, _photos, new HorlogeFixe(Maintenant), NullLogger<ExportDonneesService>.Instance);

    private async Task<(Stream Archive, string Nom)> Preparer()
    {
        var resultat = await Service().PreparerAsync(CarnetDeTest.UserId, CancellationToken.None);
        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        return resultat.Valeur;
    }

    private static JsonElement LireJson(ZipArchive zip)
    {
        using var flux = zip.GetEntry("donnees.json")!.Open();
        return JsonDocument.Parse(flux).RootElement.Clone();
    }

    public void Dispose() => _carnet.Dispose();
}
