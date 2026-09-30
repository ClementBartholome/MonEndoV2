using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Traitements;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>Historique mensuel d'un traitement : prises prévues, faites, ignorées, au besoin, séances, cloisonnement.</summary>
public sealed class HistoriqueTraitementsServiceTests : IDisposable
{
    private static readonly DateOnly Septembre = new(2026, 9, 1);
    private static readonly DateOnly Jour = new(2026, 9, 15);

    private readonly CarnetDeTest _carnet = new();

    private TraitementsService Traitements() =>
        new(_carnet.Context, _carnet.CarnetSanteService, new HorlogeFixe(new DateTimeOffset(2026, 9, 15, 10, 0, 0, TimeSpan.Zero)));

    private HistoriqueTraitementsService Service() => new(_carnet.Context, Traitements());

    private async Task<int> Creer(TraitementDto dto) => (await Traitements().CreerAsync(CarnetDeTest.UserId, dto, CancellationToken.None)).Valeur;

    private static TraitementDto Quotidien() => new()
    {
        Nom = "Diénogest", Type = TypeTraitement.Medicamenteux, Frequence = FrequencePrise.ChaqueJour,
        Horaires = [new TimeOnly(8, 0), new TimeOnly(20, 0)], DateDebut = new DateOnly(2026, 8, 1),
    };

    private async Task Prise(int id, DateTime date, StatutPrise statut = StatutPrise.Pris, TimeOnly? heure = null) =>
        await Traitements().NoterPriseAsync(CarnetDeTest.UserId, id, new PriseDto { Date = date, Statut = statut, HeurePrevue = heure }, CancellationToken.None);

    [Fact]
    public async Task TraitementPlanifie_PrevuesFaitesIgnoreesEtMoisPrecedent()
    {
        var id = await Creer(Quotidien());
        await Prise(id, new DateTime(2026, 9, 14, 8, 5, 0), heure: new TimeOnly(8, 0));
        await Prise(id, new DateTime(2026, 9, 14, 21, 0, 0), StatutPrise.Ignore, new TimeOnly(20, 0));
        await Prise(id, new DateTime(2026, 9, 2, 8, 10, 0), heure: new TimeOnly(8, 0));
        await Prise(id, new DateTime(2026, 8, 20, 8, 0, 0), heure: new TimeOnly(8, 0));

        var vue = (await Service().GetAsync(CarnetDeTest.UserId, id, Septembre, Jour, CancellationToken.None)).Valeur!;

        // 15 jours de septembre (jusqu'au jour demandé) × 2 horaires.
        Assert.Equal(30, vue.Prevues);
        Assert.Equal(2, vue.Faites);
        Assert.Equal(1, vue.Ignorees);
        Assert.Equal(1, vue.FaitesMoisPrecedent);
        Assert.Equal(["2026-09-14", "2026-09-02"], vue.Jours.Select(j => j.Jour));
        Assert.Equal(["Ignore", "Pris"], vue.Jours[0].Entrees.Select(e => e.Nature));
        Assert.Equal("08:00", vue.Jours[0].Entrees[1].HeurePrevue);
    }

    [Fact]
    public async Task TraitementArrete_GardeSesPrisesPrevuesJusquASaFin()
    {
        var id = await Creer(Quotidien());
        await Traitements().ArreterAsync(CarnetDeTest.UserId, id, new DateOnly(2026, 9, 10), CancellationToken.None);

        var vue = (await Service().GetAsync(CarnetDeTest.UserId, id, Septembre, Jour, CancellationToken.None)).Valeur!;

        Assert.Equal(20, vue.Prevues);
    }

    [Fact]
    public async Task AuBesoinEtSoin_SansPrisesPrevues()
    {
        var auBesoin = await Creer(new TraitementDto { Nom = "Ibuprofène", Type = TypeTraitement.Medicamenteux, Frequence = FrequencePrise.AuBesoin, DateDebut = new DateOnly(2026, 1, 1) });
        await Prise(auBesoin, new DateTime(2026, 9, 3, 14, 0, 0));
        await Prise(auBesoin, new DateTime(2026, 9, 12, 22, 0, 0));
        var kine = await Creer(new TraitementDto { Nom = "Kiné", Type = TypeTraitement.NonMedicamenteux, Frequence = FrequencePrise.AuBesoin, DateDebut = new DateOnly(2026, 1, 1) });
        await Traitements().NoterSeanceAsync(CarnetDeTest.UserId, kine, new SeanceDto { Date = new DateTime(2026, 9, 8, 17, 0, 0) }, CancellationToken.None);

        var vueAuBesoin = (await Service().GetAsync(CarnetDeTest.UserId, auBesoin, Septembre, Jour, CancellationToken.None)).Valeur!;
        var vueKine = (await Service().GetAsync(CarnetDeTest.UserId, kine, Septembre, Jour, CancellationToken.None)).Valeur!;

        Assert.Equal((0, 2), (vueAuBesoin.Prevues, vueAuBesoin.Faites));
        Assert.Null(vueAuBesoin.Jours[0].Entrees[0].HeurePrevue);
        Assert.Equal((0, 1), (vueKine.Prevues, vueKine.Faites));
        Assert.Equal("Seance", vueKine.Jours[0].Entrees[0].Nature);
    }

    [Fact]
    public async Task TraitementDUnAutreCarnet_Interdit()
    {
        var id = await Creer(Quotidien());

        var resultat = await Service().GetAsync(CarnetDeTest.AutreUserId, id, Septembre, Jour, CancellationToken.None);

        Assert.Equal(StatutOperation.Interdit, resultat.Statut);
        Assert.Equal(StatutOperation.Introuvable, (await Service().GetAsync(CarnetDeTest.UserId, 999, Septembre, Jour, CancellationToken.None)).Statut);
    }

    [Fact]
    public async Task AnnulerSeance_SeulementDansSonCarnet()
    {
        var kine = await Creer(new TraitementDto { Nom = "Kiné", Type = TypeTraitement.NonMedicamenteux, Frequence = FrequencePrise.AuBesoin, DateDebut = new DateOnly(2026, 1, 1) });
        var seance = (await Traitements().NoterSeanceAsync(CarnetDeTest.UserId, kine, new SeanceDto { Date = new DateTime(2026, 9, 8) }, CancellationToken.None)).Valeur;

        Assert.Equal(StatutOperation.Interdit, (await Traitements().AnnulerSeanceAsync(CarnetDeTest.AutreUserId, seance, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Succes, (await Traitements().AnnulerSeanceAsync(CarnetDeTest.UserId, seance, CancellationToken.None)).Statut);
        Assert.Empty(_carnet.Context.DonneesTraitementNonMedicamenteux);
    }

    public void Dispose() => _carnet.Dispose();
}
