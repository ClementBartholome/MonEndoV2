using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Douleurs;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>
/// Zones de douleur du bilan : remplacées à chaque enregistrement, jamais les crises ; bilan exigé ; validation ; cloisonnement ;
/// suppression et changement de jour du bilan.
/// </summary>
public sealed class DouleursDuJourServiceTests : IDisposable
{
    private static readonly DateOnly Jour = new(2026, 10, 25);
    private static readonly HorlogeFixe Maintenant = new(new DateTimeOffset(2026, 10, 25, 19, 0, 0, TimeSpan.Zero));
    private const int C = CarnetDeTest.CarnetSanteId;

    private readonly CarnetDeTest _carnet = new();

    private DouleursDuJourService Service() => new(_carnet.Context, Maintenant);

    private static ZonesDouleurBilanDto Zones(params (string Type, int Intensite)[] zones) =>
        new() { Zones = zones.Select(z => new ZoneDouleurDto { TypeDouleur = z.Type, Intensite = z.Intensite }).ToList() };

    private async Task<BilanQuotidien> BilanDuJour(int carnet = C, DateOnly? jour = null)
    {
        var bilan = new BilanQuotidien { CarnetSanteId = carnet, Date = (jour ?? Jour).ToDateTime(new TimeOnly(12, 0)), DouleurMoyenne = 6 };
        _carnet.Context.BilansQuotidiens.Add(bilan);
        await _carnet.Context.SaveChangesAsync();
        return bilan;
    }

    private async Task<DonneesDouleur> Crise(string type, int intensite, int heure, int carnet = C)
    {
        var crise = new DonneesDouleur { CarnetSanteId = carnet, TypeDouleur = type, Intensite = intensite, Date = Jour.ToDateTime(new TimeOnly(heure, 20)) };
        _carnet.Context.DonneesDouleurs.Add(crise);
        await _carnet.Context.SaveChangesAsync();
        return crise;
    }

    [Fact]
    public async Task EnregistrerZones_RemplaceLesZonesSansToucherAuxCrises()
    {
        await BilanDuJour();
        await Crise("Douleur pelvienne", 7, 14);

        Assert.Equal(StatutOperation.Succes, (await Service().EnregistrerZonesAsync(CarnetDeTest.UserId, Jour,
            Zones(("Douleur lombaire", 6), ("Douleur abdominale", 4)), CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Succes, (await Service().EnregistrerZonesAsync(CarnetDeTest.UserId, Jour,
            Zones(("Douleur lombaire", 5)), CancellationToken.None)).Statut);

        var entrees = await _carnet.Context.DonneesDouleurs.AsNoTracking().OrderBy(d => d.TypeDouleur).ToListAsync();
        Assert.Equal(2, entrees.Count);
        var zone = Assert.Single(entrees, d => d.DepuisBilan);
        Assert.Equal(("Douleur lombaire", 5, Jour.ToDateTime(new TimeOnly(12, 0))), (zone.TypeDouleur, zone.Intensite, zone.Date));
        var crise = Assert.Single(entrees, d => !d.DepuisBilan);
        Assert.Equal(("Douleur pelvienne", 7), (crise.TypeDouleur, crise.Intensite));
    }

    [Fact]
    public async Task Get_SepareZonesEtCrisesDuJour()
    {
        await BilanDuJour();
        await Crise("Douleur pelvienne", 7, 14);
        await Service().EnregistrerZonesAsync(CarnetDeTest.UserId, Jour, Zones(("Douleur lombaire", 6)), CancellationToken.None);
        // La veille et l'autre carnet ne comptent pas.
        _carnet.Context.DonneesDouleurs.AddRange(
            new DonneesDouleur { CarnetSanteId = C, TypeDouleur = "Douleur pelvienne", Intensite = 3, Date = Jour.AddDays(-1).ToDateTime(new TimeOnly(9, 0)) },
            new DonneesDouleur { CarnetSanteId = CarnetDeTest.AutreCarnetSanteId, TypeDouleur = "Autre", Intensite = 9, Date = Jour.ToDateTime(new TimeOnly(9, 0)) });
        await _carnet.Context.SaveChangesAsync();

        var resultat = await Service().GetAsync(CarnetDeTest.UserId, Jour, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        var zone = Assert.Single(resultat.Valeur!.Zones);
        Assert.Equal(("Douleur lombaire", 6), (zone.TypeDouleur, zone.Intensite));
        var crise = Assert.Single(resultat.Valeur.Crises);
        Assert.Equal(("Douleur pelvienne", 7, "14:20"), (crise.TypeDouleur, crise.Intensite, crise.Heure));
    }

    [Fact]
    public async Task EnregistrerZones_SansBilanCeJourLa_Introuvable()
    {
        // Un bilan de l'autre carnet ce jour-là ne suffit pas.
        await BilanDuJour(CarnetDeTest.AutreCarnetSanteId);

        var resultat = await Service().EnregistrerZonesAsync(CarnetDeTest.UserId, Jour, Zones(("Douleur lombaire", 6)), CancellationToken.None);

        Assert.Equal(StatutOperation.Introuvable, resultat.Statut);
        Assert.Empty(_carnet.Context.DonneesDouleurs);
    }

    [Fact]
    public async Task EnregistrerZones_NeTouchePasAuxZonesDUnAutreCarnet()
    {
        await BilanDuJour();
        _carnet.Context.DonneesDouleurs.Add(new DonneesDouleur
        {
            CarnetSanteId = CarnetDeTest.AutreCarnetSanteId, TypeDouleur = "Douleur lombaire", Intensite = 4,
            Date = Jour.ToDateTime(new TimeOnly(12, 0)), DepuisBilan = true,
        });
        await _carnet.Context.SaveChangesAsync();

        await Service().EnregistrerZonesAsync(CarnetDeTest.UserId, Jour, Zones(), CancellationToken.None);

        Assert.Single(_carnet.Context.DonneesDouleurs, d => d.CarnetSanteId == CarnetDeTest.AutreCarnetSanteId);
    }

    public static TheoryData<ZonesDouleurBilanDto> ZonesInvalides => new()
    {
        Zones(("Douleur lombaire", 0)),
        Zones(("Douleur lombaire", 11)),
        Zones(("  ", 5)),
        Zones((new string('a', 51), 5)),
        Zones(("Douleur lombaire", 5), ("douleur lombaire ", 6)),
        Zones(Enumerable.Range(0, 9).Select(i => ($"Type {i}", 5)).ToArray()),
    };

    [Theory]
    [MemberData(nameof(ZonesInvalides))]
    public async Task EnregistrerZones_Invalides_Refusees(ZonesDouleurBilanDto zones)
    {
        await BilanDuJour();

        var resultat = await Service().EnregistrerZonesAsync(CarnetDeTest.UserId, Jour, zones, CancellationToken.None);

        Assert.Equal(StatutOperation.Invalide, resultat.Statut);
        Assert.Empty(_carnet.Context.DonneesDouleurs);
    }

    [Fact]
    public async Task EnregistrerZones_JourAVenir_Refuse()
    {
        var resultat = await Service().EnregistrerZonesAsync(CarnetDeTest.UserId, Jour.AddDays(3), Zones(("Douleur lombaire", 5)), CancellationToken.None);

        Assert.Equal(StatutOperation.Invalide, resultat.Statut);
    }

    [Fact]
    public async Task SupprimerLeBilan_RetireSesZonesEtGardeLesCrises()
    {
        var bilan = await BilanDuJour();
        await Crise("Douleur pelvienne", 7, 14);
        await Service().EnregistrerZonesAsync(CarnetDeTest.UserId, Jour, Zones(("Douleur lombaire", 6)), CancellationToken.None);

        var reponse = await Controleur().DeleteBilanQuotidien(bilan.Id);

        Assert.IsType<NoContentResult>(reponse);
        var restante = Assert.Single(_carnet.Context.DonneesDouleurs.AsNoTracking());
        Assert.False(restante.DepuisBilan);
    }

    [Fact]
    public async Task ChangerLeJourDuBilan_DeplaceSesZones()
    {
        var bilan = await BilanDuJour();
        await Service().EnregistrerZonesAsync(CarnetDeTest.UserId, Jour, Zones(("Douleur lombaire", 6)), CancellationToken.None);
        var veille = Jour.AddDays(-1);
        _carnet.Context.Entry(bilan).State = EntityState.Detached;

        var modifie = new BilanQuotidien
        {
            Id = bilan.Id, CarnetSanteId = C, Date = veille.ToDateTime(new TimeOnly(12, 0)), DouleurMoyenne = 6,
            Emotions = [new EmotionBilan { Emotion = Emotion.Calme }],
        };
        var reponse = await Controleur().PutBilanQuotidien(bilan.Id, modifie);

        Assert.IsType<NoContentResult>(reponse);
        var zone = Assert.Single(_carnet.Context.DonneesDouleurs.AsNoTracking());
        Assert.Equal(veille.ToDateTime(new TimeOnly(12, 0)), zone.Date);
    }

    private BilanQuotidienController Controleur() =>
        new(_carnet.Context, _carnet.CarnetSanteService, Maintenant, Service()) { ControllerContext = CarnetDeTest.ContexteAuthentifie() };

    public void Dispose() => _carnet.Dispose();
}
