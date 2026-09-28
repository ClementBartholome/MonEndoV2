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
    public async Task ExportPdf_PriseIgnoree_NApparaitPasCommeUnePrise()
    {
        var traitement = new Medicament { CarnetSanteId = CarnetDeTest.CarnetSanteId, Nom = "Diénogest", Type = TypeTraitement.Medicamenteux };
        _carnet.Context.Medicaments.Add(traitement);
        await _carnet.Context.SaveChangesAsync();
        _carnet.Context.DonneesMedicaments.AddRange(
            new DonneesMedicament { CarnetSanteId = CarnetDeTest.CarnetSanteId, MedicamentId = traitement.Id, NombreComprimes = 1, Date = new DateTime(2026, 9, 26, 8, 0, 0) },
            new DonneesMedicament { CarnetSanteId = CarnetDeTest.CarnetSanteId, MedicamentId = traitement.Id, Statut = StatutPrise.Ignore, Date = new DateTime(2026, 9, 27, 8, 0, 0) });
        await _carnet.Context.SaveChangesAsync();
        _carnet.Context.ChangeTracker.Clear();

        var export = await _carnet.CarnetSanteService.GetDonneesCarnetSanteByMonthForPdf(
            CarnetDeTest.CarnetSanteId, 9, 2026, CarnetDeTest.UserId);

        Assert.Single(export.DonneesMedicament);
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

    [Theory]
    [InlineData(null, null, null)]
    [InlineData(4, null, 4.0)]
    [InlineData(1, 2, 1.5)]
    public async Task ExportPdf_StressMoyen_IgnoreLesValeursNonRenseignees(int? stressPro, int? stressPerso, double? attendu)
    {
        _carnet.Context.BilansQuotidiens.Add(new BilanQuotidien
        {
            CarnetSanteId = CarnetDeTest.CarnetSanteId,
            Date = new DateTime(2026, 9, 26),
            Mood = "Neutre",
            StressPro = stressPro,
            StressPerso = stressPerso,
        });
        await _carnet.Context.SaveChangesAsync();

        var export = await _carnet.CarnetSanteService.GetDonneesCarnetSanteByMonthForPdf(
            CarnetDeTest.CarnetSanteId, 9, 2026, CarnetDeTest.UserId);

        var bilan = Assert.Single(export.BilansQuotidiens);
        Assert.Equal(attendu, bilan.StressMoyenne);
        Assert.Null(bilan.Pas);
        Assert.Null(bilan.Hydratation);
    }

    public void Dispose() => _carnet.Dispose();
}
