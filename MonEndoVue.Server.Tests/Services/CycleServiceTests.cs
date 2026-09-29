using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Cycle;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>Règles de la session : mois, cycle en cours, historique, ajout et retrait d'un jour, cloisonnement.</summary>
public sealed class CycleServiceTests : IDisposable
{
    private static readonly DateOnly Jour = new(2026, 9, 15);
    private static readonly DateOnly Septembre = new(2026, 9, 1);
    private static readonly DateTimeOffset Maintenant = new(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly CarnetDeTest _carnet = new();

    private CycleService Service() => new(_carnet.Context, _carnet.CarnetSanteService, new HorlogeFixe(Maintenant));

    private async Task Noter(int carnetId, params DateTime[] dates)
    {
        _carnet.Context.JourRegles.AddRange(dates.Select(d => new JourRegle { CarnetSanteId = carnetId, Date = d }));
        await _carnet.Context.SaveChangesAsync();
    }

    [Fact]
    public async Task Get_MoisCycleEnCoursEtHistorique_DuSeulCarnetDeLaSession()
    {
        await Noter(CarnetDeTest.CarnetSanteId,
            new DateTime(2026, 7, 14), new DateTime(2026, 7, 15),
            new DateTime(2026, 8, 14), new DateTime(2026, 8, 15), new DateTime(2026, 8, 16),
            // Une ancienne saisie avec une heure compte pour son jour.
            new DateTime(2026, 9, 14, 8, 30, 0), new DateTime(2026, 9, 15));
        await Noter(CarnetDeTest.AutreCarnetSanteId, new DateTime(2026, 9, 1), new DateTime(2026, 9, 2));

        var resultat = await Service().GetAsync(CarnetDeTest.UserId, Jour, Septembre, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        var vue = resultat.Valeur!;
        Assert.Equal(["2026-09-14", "2026-09-15"], vue.JoursDeRegles);
        Assert.Equal("2026-09-14", vue.EnCours!.Debut);
        Assert.Equal(2, vue.EnCours.JourDuCycle);
        Assert.Equal(2, vue.EnCours.JourDeRegles);
        Assert.Equal(["2026-08-14", "2026-07-14"], vue.Cycles.Select(c => c.Debut));
        Assert.Equal([31, 31], vue.Cycles.Select(c => c.Duree));
        Assert.Equal([3, 2], vue.Cycles.Select(c => c.JoursDeRegles));
        Assert.Equal(31, vue.DureeMoyenne);
    }

    [Fact]
    public async Task Get_SansRegles_NiCycleEnCoursNiMoyenne()
    {
        var vue = (await Service().GetAsync(CarnetDeTest.UserId, Jour, Septembre, CancellationToken.None)).Valeur!;

        Assert.Empty(vue.JoursDeRegles);
        Assert.Null(vue.EnCours);
        Assert.Empty(vue.Cycles);
        Assert.Null(vue.DureeMoyenne);
    }

    [Fact]
    public async Task Get_JourTropEloigneDuServeur_Refuse()
    {
        var resultat = await Service().GetAsync(CarnetDeTest.UserId, Jour.AddDays(5), Septembre, CancellationToken.None);

        Assert.Equal(StatutOperation.Invalide, resultat.Statut);
    }

    [Fact]
    public async Task AjouterJour_UneSeuleFois_PuisRetirer()
    {
        var service = Service();

        await service.AjouterJourAsync(CarnetDeTest.UserId, Jour, CancellationToken.None);
        await service.AjouterJourAsync(CarnetDeTest.UserId, Jour, CancellationToken.None);
        var notes = await _carnet.Context.JourRegles.Where(j => j.CarnetSanteId == CarnetDeTest.CarnetSanteId).ToListAsync();
        Assert.Single(notes);
        Assert.Equal(new DateTime(2026, 9, 15), notes[0].Date);

        Assert.Equal(StatutOperation.Succes, (await service.RetirerJourAsync(CarnetDeTest.UserId, Jour, CancellationToken.None)).Statut);
        Assert.Empty(await _carnet.Context.JourRegles.ToListAsync());
    }

    [Fact]
    public async Task AjouterJour_AVenir_Refuse()
    {
        var resultat = await Service().AjouterJourAsync(CarnetDeTest.UserId, Jour.AddDays(3), CancellationToken.None);

        Assert.Equal(StatutOperation.Invalide, resultat.Statut);
        Assert.Empty(await _carnet.Context.JourRegles.ToListAsync());
    }

    [Fact]
    public async Task RetirerJour_NeTouchePasAUnAutreCarnet()
    {
        await Noter(CarnetDeTest.AutreCarnetSanteId, new DateTime(2026, 9, 15));

        await Service().RetirerJourAsync(CarnetDeTest.UserId, Jour, CancellationToken.None);

        Assert.Single(await _carnet.Context.JourRegles.Where(j => j.CarnetSanteId == CarnetDeTest.AutreCarnetSanteId).ToListAsync());
    }

    [Fact]
    public async Task SansCarnet_NonAuthentifie()
    {
        Assert.Equal(StatutOperation.NonAuthentifie, (await Service().GetAsync("inconnue", Jour, Septembre, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.NonAuthentifie, (await Service().AjouterJourAsync("", Jour, CancellationToken.None)).Statut);
    }

    public void Dispose() => _carnet.Dispose();
}
