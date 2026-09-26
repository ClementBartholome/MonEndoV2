using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services.WebPush.Rappels;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

public sealed class ReglesRappelTests : IDisposable
{
    private static readonly DateOnly Aujourdhui = new(2026, 9, 26);
    private static readonly TimeZoneInfo Paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

    private readonly CarnetDeTest _carnet = new();

    private void Symptome(int carnetSanteId, string type, DateTime dateUtc, string? photoUrl)
    {
        _carnet.Context.SymptomesCycles.Add(new SymptomeCycle
        {
            CarnetSanteId = carnetSanteId,
            TypeSymptome = type,
            Date = dateUtc,
            Intensite = 3,
            PhotoUrl = photoUrl,
        });
        _carnet.Context.SaveChanges();
    }

    private Task<bool> AcneSuivie() => new RappelSuiviAcne(_carnet.Context)
        .SuiviDejaFaitAsync(CarnetDeTest.CarnetSanteId, Aujourdhui, Paris, CancellationToken.None);

    [Fact]
    public async Task Acne_PhotoIlYA3Jours_SuiviDejaFait()
    {
        Symptome(CarnetDeTest.CarnetSanteId, "Acné", new DateTime(2026, 9, 23, 10, 0, 0), "https://photos/1.jpg");

        Assert.True(await AcneSuivie());
    }

    [Fact]
    public async Task Acne_PhotoIlYA8Jours_SuiviAFaire()
    {
        Symptome(CarnetDeTest.CarnetSanteId, "Acné", new DateTime(2026, 9, 18, 10, 0, 0), "https://photos/1.jpg");

        Assert.False(await AcneSuivie());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Acne_EntreeRecenteSansPhoto_SuiviAFaire(string? photoUrl)
    {
        Symptome(CarnetDeTest.CarnetSanteId, "Acné", new DateTime(2026, 9, 25, 10, 0, 0), photoUrl);

        Assert.False(await AcneSuivie());
    }

    [Fact]
    public async Task Acne_PhotoDUnAutreSymptomeOuDUnAutreCarnet_Ignoree()
    {
        Symptome(CarnetDeTest.CarnetSanteId, "Migraine", new DateTime(2026, 9, 25, 10, 0, 0), "https://photos/1.jpg");
        Symptome(CarnetDeTest.AutreCarnetSanteId, "Acné", new DateTime(2026, 9, 25, 10, 0, 0), "https://photos/2.jpg");

        Assert.False(await AcneSuivie());
    }

    [Fact]
    public async Task Bilan_RempliDansLaJourneeLocale_SuiviDejaFait()
    {
        var regle = new RappelBilanQuotidien(_carnet.Context);
        Assert.False(await regle.SuiviDejaFaitAsync(CarnetDeTest.CarnetSanteId, Aujourdhui, Paris, CancellationToken.None));

        // Minuit à Paris, enregistré en UTC la veille à 22h.
        _carnet.Context.BilansQuotidiens.Add(new BilanQuotidien
        {
            CarnetSanteId = CarnetDeTest.CarnetSanteId,
            Date = new DateTime(2026, 9, 25, 22, 0, 0),
            Mood = "Neutre",
        });
        _carnet.Context.SaveChanges();

        Assert.True(await regle.SuiviDejaFaitAsync(CarnetDeTest.CarnetSanteId, Aujourdhui, Paris, CancellationToken.None));
    }

    [Fact]
    public void Regles_ExposentLeurCalendrierParDefautEtLaPageAOuvrir()
    {
        var bilan = new RappelBilanQuotidien(_carnet.Context);
        var acne = new RappelSuiviAcne(_carnet.Context);

        Assert.Equal((TypeRappel.BilanQuotidien, false, new TimeOnly(21, 0), (DayOfWeek?)null, "/bilan-quotidien"),
            (bilan.Type, bilan.EstHebdomadaire, bilan.HeureParDefaut, bilan.JourParDefaut, bilan.Message.Url));
        Assert.Equal((TypeRappel.SuiviAcne, true, new TimeOnly(20, 0), (DayOfWeek?)DayOfWeek.Sunday, "/cycle?onglet=acne"),
            (acne.Type, acne.EstHebdomadaire, acne.HeureParDefaut, acne.JourParDefaut, acne.Message.Url));
    }

    [Fact]
    public void JourneeEnUtc_JourneeParisienneEnHeureDEte_CommenceLaVeilleA22hUtc()
    {
        var (debut, fin) = FuseauxHoraires.JourneeEnUtc(Aujourdhui, Paris);

        Assert.Equal(new DateTime(2026, 9, 25, 22, 0, 0), debut);
        Assert.Equal(new DateTime(2026, 9, 26, 22, 0, 0), fin);
    }

    [Theory]
    [InlineData("Europe/Paris", true)]
    [InlineData("America/New_York", true)]
    [InlineData("Fuseau/Inexistant", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void EstValide_ReconnaitLesFuseauxIana(string? fuseau, bool attendu)
    {
        Assert.Equal(attendu, FuseauxHoraires.EstValide(fuseau));
    }

    public void Dispose() => _carnet.Dispose();
}
