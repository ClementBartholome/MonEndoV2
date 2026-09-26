using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Controllers;

public sealed class BilanQuotidienControllerEmotionsTests : IDisposable
{
    private readonly CarnetDeTest _carnet = new();
    private readonly BilanQuotidienController _controller;

    public BilanQuotidienControllerEmotionsTests()
    {
        _controller = new BilanQuotidienController(_carnet.Context, _carnet.CarnetSanteService)
        {
            ControllerContext = CarnetDeTest.ContexteAuthentifie(),
        };
    }

    private static BilanQuotidien Bilan(int carnetSanteId = CarnetDeTest.CarnetSanteId, string? mood = null, params Emotion[] emotions) => new()
    {
        CarnetSanteId = carnetSanteId,
        Date = new DateTime(2026, 9, 26),
        Mood = mood,
        Emotions = emotions.Select(e => new EmotionBilan { Emotion = e }).ToList(),
    };

    private BilanQuotidien Enregistrer(BilanQuotidien bilan)
    {
        _carnet.Context.BilansQuotidiens.Add(bilan);
        _carnet.Context.SaveChanges();
        _carnet.Context.ChangeTracker.Clear();
        return bilan;
    }

    private List<Emotion> EmotionsEnBase(int bilanId) => _carnet.Context.BilansQuotidiens.AsNoTracking()
        .Single(b => b.Id == bilanId).Emotions.Select(e => e.Emotion).Order().ToList();

    [Fact]
    public async Task Post_AvecEmotions_LesEnregistreEnIgnorantLeursIdentifiants()
    {
        var bilan = Bilan(emotions: [Emotion.Joie, Emotion.Anxiete]);
        bilan.Emotions[0].Id = 999;

        var resultat = await _controller.PostBilanQuotidien(bilan);

        Assert.IsType<CreatedAtActionResult>(resultat.Result);
        _carnet.Context.ChangeTracker.Clear();
        var enregistre = Assert.Single(_carnet.Context.BilansQuotidiens);
        Assert.Equal([Emotion.Joie, Emotion.Anxiete], EmotionsEnBase(enregistre.Id));
        Assert.DoesNotContain(enregistre.Emotions, e => e.Id == 999);
    }

    [Fact]
    public async Task Post_SansEmotion_RetourneBadRequestSansEnregistrer()
    {
        var resultat = await _controller.PostBilanQuotidien(Bilan());

        Assert.IsType<BadRequestObjectResult>(resultat.Result);
        Assert.Empty(_carnet.Context.BilansQuotidiens);
    }

    [Fact]
    public async Task Put_RemplaceLesEmotionsDuBilan()
    {
        var bilan = Enregistrer(Bilan(emotions: [Emotion.Tristesse, Emotion.Calme]));

        var resultat = await _controller.PutBilanQuotidien(bilan.Id, new BilanQuotidien
        {
            Id = bilan.Id,
            CarnetSanteId = CarnetDeTest.CarnetSanteId,
            Date = bilan.Date,
            Emotions = [new EmotionBilan { Emotion = Emotion.Calme }, new EmotionBilan { Emotion = Emotion.Motivation }],
        });

        Assert.IsType<NoContentResult>(resultat);
        _carnet.Context.ChangeTracker.Clear();
        Assert.Equal([Emotion.Calme, Emotion.Motivation], EmotionsEnBase(bilan.Id));
    }

    [Fact]
    public async Task Put_AncienBilanSansEmotion_ConserveSonHumeur()
    {
        var bilan = Enregistrer(Bilan(mood: "Heureuse"));

        var resultat = await _controller.PutBilanQuotidien(bilan.Id, new BilanQuotidien
        {
            Id = bilan.Id, CarnetSanteId = CarnetDeTest.CarnetSanteId, Date = bilan.Date, Mood = "Heureuse", Fatigue = 2,
        });

        Assert.IsType<NoContentResult>(resultat);
        _carnet.Context.ChangeTracker.Clear();
        var enregistre = _carnet.Context.BilansQuotidiens.Single();
        Assert.Equal(("Heureuse", 2), (enregistre.Mood, enregistre.Fatigue));
        Assert.Empty(enregistre.Emotions);
    }

    [Fact]
    public async Task Put_BilanDUnAutreCarnet_EstInterditEtNeModifiePasSesEmotions()
    {
        var bilan = Enregistrer(Bilan(CarnetDeTest.AutreCarnetSanteId, emotions: [Emotion.Joie]));

        var resultat = await _controller.PutBilanQuotidien(bilan.Id, new BilanQuotidien
        {
            Id = bilan.Id, CarnetSanteId = CarnetDeTest.CarnetSanteId, Date = bilan.Date,
            Emotions = [new EmotionBilan { Emotion = Emotion.Frustration }],
        });

        Assert.IsType<ForbidResult>(resultat);
        _carnet.Context.ChangeTracker.Clear();
        Assert.Equal([Emotion.Joie], EmotionsEnBase(bilan.Id));
    }

    [Fact]
    public async Task Get_RenvoieLesEmotionsAvecLeBilan()
    {
        var bilan = Enregistrer(Bilan(emotions: [Emotion.Fierte]));

        var resultat = await _controller.GetBilanQuotidien(bilan.Id);

        Assert.Equal(Emotion.Fierte, Assert.Single(resultat.Value!.Emotions).Emotion);
    }

    public void Dispose() => _carnet.Dispose();
}
