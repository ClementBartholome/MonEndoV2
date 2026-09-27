using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Tests.Support;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>Historique des bilans par période : bornes, cloisonnement et jours de règles.</summary>
public sealed class HistoriqueBilansServiceTests : IDisposable
{
    private static readonly DateOnly Du = new(2026, 9, 1);
    private static readonly DateOnly Au = new(2026, 9, 30);

    private readonly CarnetDeTest _carnet = new();
    private readonly HistoriqueBilansService _service;

    public HistoriqueBilansServiceTests()
    {
        _service = new HistoriqueBilansService(_carnet.Context);
    }

    private void AjouterBilan(DateTime date, int carnetSanteId = CarnetDeTest.CarnetSanteId, string? mood = null,
        params Emotion[] emotions)
    {
        _carnet.Context.BilansQuotidiens.Add(new BilanQuotidien
        {
            CarnetSanteId = carnetSanteId,
            Date = date,
            DouleurMoyenne = 4,
            Mood = mood,
            Emotions = emotions.Select(e => new EmotionBilan { Emotion = e }).ToList(),
        });
        _carnet.Context.SaveChanges();
    }

    private void AjouterJourRegle(DateTime date, int carnetSanteId = CarnetDeTest.CarnetSanteId)
    {
        _carnet.Context.JourRegles.Add(new JourRegle { CarnetSanteId = carnetSanteId, Date = date });
        _carnet.Context.SaveChanges();
    }

    [Fact]
    public async Task GetPeriode_RetourneLesBilansDeLaPeriodeTriesParDate_BornesIncluses()
    {
        AjouterBilan(new DateTime(2026, 9, 30, 23, 0, 0));
        AjouterBilan(new DateTime(2026, 9, 1, 12, 0, 0));
        AjouterBilan(new DateTime(2026, 8, 31, 12, 0, 0));
        AjouterBilan(new DateTime(2026, 10, 1, 0, 0, 0));

        var resultat = await _service.GetPeriodeAsync(CarnetDeTest.UserId, Du, Au, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        Assert.Equal(
            [new DateTime(2026, 9, 1, 12, 0, 0), new DateTime(2026, 9, 30, 23, 0, 0)],
            resultat.Valeur!.Bilans.Select(b => b.Date));
    }

    [Fact]
    public async Task GetPeriode_NeRetourneJamaisLesDonneesDUnAutreCarnet()
    {
        AjouterBilan(new DateTime(2026, 9, 10, 12, 0, 0), CarnetDeTest.AutreCarnetSanteId);
        AjouterJourRegle(new DateTime(2026, 9, 10), CarnetDeTest.AutreCarnetSanteId);

        var resultat = await _service.GetPeriodeAsync(CarnetDeTest.UserId, Du, Au, CancellationToken.None);

        Assert.Empty(resultat.Valeur!.Bilans);
        Assert.Empty(resultat.Valeur.JoursRegles);
    }

    [Fact]
    public async Task GetPeriode_CopieLesMesuresEmotionsEtAncienneHumeur()
    {
        AjouterBilan(new DateTime(2026, 9, 2, 12, 0, 0), emotions: [Emotion.Calme, Emotion.Fierte]);
        AjouterBilan(new DateTime(2026, 9, 3, 12, 0, 0), mood: "Heureuse");

        var bilans = (await _service.GetPeriodeAsync(CarnetDeTest.UserId, Du, Au, CancellationToken.None)).Valeur!.Bilans;

        Assert.Equal([Emotion.Calme, Emotion.Fierte], bilans[0].Emotions.Select(e => e.Emotion).Order());
        Assert.Equal(4, bilans[0].DouleurMoyenne);
        Assert.Null(bilans[0].Fatigue);
        Assert.Equal(CarnetDeTest.CarnetSanteId, bilans[0].CarnetSanteId);
        Assert.Equal("Heureuse", bilans[1].Mood);
        Assert.Empty(bilans[1].Emotions);
    }

    [Fact]
    public async Task GetPeriode_RetourneLesJoursDeReglesSansDoublonNiHeure()
    {
        AjouterJourRegle(new DateTime(2026, 9, 5));
        AjouterJourRegle(new DateTime(2026, 9, 4, 12, 0, 0));
        AjouterJourRegle(new DateTime(2026, 9, 4));
        AjouterJourRegle(new DateTime(2026, 10, 1));

        var resultat = await _service.GetPeriodeAsync(CarnetDeTest.UserId, Du, Au, CancellationToken.None);

        Assert.Equal([new DateOnly(2026, 9, 4), new DateOnly(2026, 9, 5)], resultat.Valeur!.JoursRegles);
    }

    [Theory]
    [InlineData(2026, 9, 30, 2026, 9, 1)]   // fin avant le début
    [InlineData(2026, 9, 1, 2026, 10, 13)]  // 43 jours
    public async Task GetPeriode_PeriodeInvalide_RetourneInvalide(int a1, int m1, int j1, int a2, int m2, int j2)
    {
        var resultat = await _service.GetPeriodeAsync(
            CarnetDeTest.UserId, new DateOnly(a1, m1, j1), new DateOnly(a2, m2, j2), CancellationToken.None);

        Assert.Equal(StatutOperation.Invalide, resultat.Statut);
        Assert.NotNull(resultat.Message);
    }

    [Fact]
    public async Task GetPeriode_SixSemainesEtUnSeulJour_SontAcceptes()
    {
        var sixSemaines = await _service.GetPeriodeAsync(
            CarnetDeTest.UserId, Du, Du.AddDays(HistoriqueBilansService.DureeMaximaleJours - 1), CancellationToken.None);
        var unJour = await _service.GetPeriodeAsync(CarnetDeTest.UserId, Du, Du, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, sixSemaines.Statut);
        Assert.Equal(StatutOperation.Succes, unJour.Statut);
    }

    [Theory]
    [InlineData("")]
    [InlineData("utilisatrice-sans-carnet")]
    public async Task GetPeriode_SansCarnet_RetourneNonAuthentifie(string userId)
    {
        var resultat = await _service.GetPeriodeAsync(userId, Du, Au, CancellationToken.None);

        Assert.Equal(StatutOperation.NonAuthentifie, resultat.Statut);
    }

    [Fact]
    public async Task Controleur_GetPeriode_DeduitLeCarnetDeLaSession()
    {
        AjouterBilan(new DateTime(2026, 9, 10, 12, 0, 0));
        AjouterBilan(new DateTime(2026, 9, 11, 12, 0, 0), CarnetDeTest.AutreCarnetSanteId);
        var controller = new BilanQuotidienController(_carnet.Context, _carnet.CarnetSanteService, TimeProvider.System)
        {
            ControllerContext = CarnetDeTest.ContexteAuthentifie(),
        };

        var reponse = await controller.GetPeriode(Du, Au, _service, CancellationToken.None);

        var periode = Assert.IsType<HistoriqueBilansViewModel>(Assert.IsType<OkObjectResult>(reponse).Value);
        Assert.Equal(new DateTime(2026, 9, 10, 12, 0, 0), Assert.Single(periode.Bilans).Date);
    }

    [Fact]
    public async Task Controleur_GetPeriodeInvalide_RetourneBadRequest()
    {
        var controller = new BilanQuotidienController(_carnet.Context, _carnet.CarnetSanteService, TimeProvider.System)
        {
            ControllerContext = CarnetDeTest.ContexteAuthentifie(),
        };

        var reponse = await controller.GetPeriode(Au, Du, _service, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(reponse);
    }

    public void Dispose() => _carnet.Dispose();
}
