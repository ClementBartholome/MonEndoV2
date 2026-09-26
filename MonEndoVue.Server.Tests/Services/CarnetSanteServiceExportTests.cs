using MonEndoVue.Server.Models;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

public class CarnetSanteServiceExportTests : IDisposable
{
    private readonly CarnetDeTest _carnet = new();

    [Fact]
    public async Task ExportPdf_BilanAvecTransit_ExposeLesChampsTransit()
    {
        _carnet.Context.BilansQuotidiens.Add(new BilanQuotidien
        {
            CarnetSanteId = CarnetDeTest.CarnetSanteId,
            Date = new DateTime(2026, 9, 26),
            Mood = "Neutre",
            Selles = true,
            TypeBristol = 3,
            CrampesEstomac = true,
            IntensiteCrampes = "Forte",
            Ballonnements = false,
        });
        await _carnet.Context.SaveChangesAsync();

        var export = await _carnet.CarnetSanteService.GetDonneesCarnetSanteByMonthForPdf(
            CarnetDeTest.CarnetSanteId, 9, 2026, CarnetDeTest.UserId);

        var bilan = Assert.Single(export.BilansQuotidiens);
        Assert.True(bilan.Selles);
        Assert.Equal(3, bilan.TypeBristol);
        Assert.True(bilan.CrampesEstomac);
        Assert.Equal("Forte", bilan.IntensiteCrampes);
        Assert.False(bilan.Ballonnements);
        Assert.Null(bilan.IntensiteBallonnements);
    }

    [Fact]
    public async Task ExportPdf_BilanSansTransit_LaisseLesChampsNonRenseignes()
    {
        _carnet.Context.BilansQuotidiens.Add(new BilanQuotidien
        {
            CarnetSanteId = CarnetDeTest.CarnetSanteId,
            Date = new DateTime(2026, 9, 26),
            Mood = "Neutre",
        });
        await _carnet.Context.SaveChangesAsync();

        var export = await _carnet.CarnetSanteService.GetDonneesCarnetSanteByMonthForPdf(
            CarnetDeTest.CarnetSanteId, 9, 2026, CarnetDeTest.UserId);

        var bilan = Assert.Single(export.BilansQuotidiens);
        Assert.Null(bilan.Selles);
        Assert.Null(bilan.TypeBristol);
    }

    [Fact]
    public async Task ExportPdf_ExposeLesEmotionsEtLAncienneHumeur()
    {
        _carnet.Context.BilansQuotidiens.AddRange(
            new BilanQuotidien
            {
                CarnetSanteId = CarnetDeTest.CarnetSanteId,
                Date = new DateTime(2026, 9, 25),
                Mood = "Triste",
            },
            new BilanQuotidien
            {
                CarnetSanteId = CarnetDeTest.CarnetSanteId,
                Date = new DateTime(2026, 9, 26),
                Emotions = [new EmotionBilan { Emotion = Emotion.Calme }, new EmotionBilan { Emotion = Emotion.Fierte }],
            });
        await _carnet.Context.SaveChangesAsync();

        var export = await _carnet.CarnetSanteService.GetDonneesCarnetSanteByMonthForPdf(
            CarnetDeTest.CarnetSanteId, 9, 2026, CarnetDeTest.UserId);

        var ancien = export.BilansQuotidiens.Single(b => b.Date.Day == 25);
        Assert.Equal(("Triste", 0), (ancien.Mood, ancien.Emotions.Count));
        var nouveau = export.BilansQuotidiens.Single(b => b.Date.Day == 26);
        Assert.Equal(["Calme", "Fierte"], nouveau.Emotions);
        Assert.Null(nouveau.Mood);
    }

    public void Dispose() => _carnet.Dispose();
}
