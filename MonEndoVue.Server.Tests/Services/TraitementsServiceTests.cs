using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Traitements;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>Traitements de la session : planning du jour, saisie, prises faites ou ignorées, séances, cloisonnement.</summary>
public sealed class TraitementsServiceTests : IDisposable
{
    private static readonly DateOnly Jour = new(2026, 9, 28);
    private static readonly DateTimeOffset Maintenant = new(2026, 9, 28, 9, 0, 0, TimeSpan.Zero);

    private readonly CarnetDeTest _carnet = new();

    private TraitementsService Service() => new(_carnet.Context, _carnet.CarnetSanteService, new HorlogeFixe(Maintenant));

    private static TraitementDto Quotidien(string nom = "Diénogest", params string[] horaires) => new()
    {
        Nom = nom,
        Type = TypeTraitement.Medicamenteux,
        Dose = "1 comprimé",
        Frequence = FrequencePrise.ChaqueJour,
        Horaires = (horaires.Length == 0 ? ["08:00"] : horaires).Select(TimeOnly.Parse).ToList(),
        DateDebut = Jour.AddDays(-7),
    };

    private async Task<int> Creer(TraitementDto dto)
    {
        var resultat = await Service().CreerAsync(CarnetDeTest.UserId, dto, CancellationToken.None);
        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        return resultat.Valeur;
    }

    [Fact]
    public async Task GetDuJour_RepartitPrisesPrevuesAuBesoinSoinsEtTermines()
    {
        var dienogest = await Creer(Quotidien("Diénogest", "21:00", "08:00"));
        await Creer(new TraitementDto { Nom = "Ibuprofène", Type = TypeTraitement.Medicamenteux, Frequence = FrequencePrise.AuBesoin, DateDebut = Jour.AddDays(-30) });
        var kine = await Creer(new TraitementDto { Nom = "Kiné", Type = TypeTraitement.NonMedicamenteux, Frequence = FrequencePrise.AuBesoin, DateDebut = Jour.AddDays(-30) });
        var ancien = await Creer(Quotidien("Ancien"));
        await Service().ArreterAsync(CarnetDeTest.UserId, ancien, Jour.AddDays(-1), CancellationToken.None);
        await Service().NoterPriseAsync(CarnetDeTest.UserId, dienogest, new PriseDto { Statut = StatutPrise.Pris, HeurePrevue = new TimeOnly(8, 0), Date = Jour.ToDateTime(new TimeOnly(8, 10)) }, CancellationToken.None);
        await Service().NoterSeanceAsync(CarnetDeTest.UserId, kine, new SeanceDto { Date = Jour.AddDays(-6).ToDateTime(new TimeOnly(18, 0)) }, CancellationToken.None);

        var vue = (await Service().GetDuJourAsync(CarnetDeTest.UserId, Jour, CancellationToken.None)).Valeur!;

        Assert.Equal(["08:00", "21:00"], vue.PrisesPrevues.Select(p => p.HeurePrevue));
        Assert.Equal("Pris", vue.PrisesPrevues[0].Reponse!.Statut);
        Assert.Null(vue.PrisesPrevues[1].Reponse);
        Assert.Equal("Ibuprofène", Assert.Single(vue.AuBesoin).Nom);
        Assert.Equal(Jour.AddDays(-6).ToDateTime(new TimeOnly(18, 0)), Assert.Single(vue.Soins).DerniereSeance);
        Assert.Equal("Ancien", Assert.Single(vue.Termines).Nom);
        var detail = vue.EnCours.Single(t => t.Nom == "Diénogest");
        Assert.Equal(["08:00", "21:00"], detail.Horaires);
        Assert.Equal("ChaqueJour", detail.Frequence);
    }

    [Fact]
    public async Task NoterPrise_NouvelleReponseAuMemeHoraire_RemplaceLaPrecedente()
    {
        var id = await Creer(Quotidien());
        var dix = Jour.ToDateTime(new TimeOnly(10, 0));
        await Service().NoterPriseAsync(CarnetDeTest.UserId, id, new PriseDto { Statut = StatutPrise.Pris, HeurePrevue = new TimeOnly(8, 0), Date = dix }, CancellationToken.None);
        await Service().NoterPriseAsync(CarnetDeTest.UserId, id, new PriseDto { Statut = StatutPrise.Ignore, HeurePrevue = new TimeOnly(8, 0), Date = dix }, CancellationToken.None);

        var prise = await _carnet.Context.DonneesMedicaments.SingleAsync();
        Assert.Equal(StatutPrise.Ignore, prise.Statut);
        Assert.Equal(0, prise.NombreComprimes);
    }

    [Fact]
    public async Task AnnulerPrise_SupprimeLaReponse_SeulementDansSonCarnet()
    {
        var id = await Creer(Quotidien());
        var priseId = (await Service().NoterPriseAsync(CarnetDeTest.UserId, id, new PriseDto { Statut = StatutPrise.Pris, Date = Jour.ToDateTime(new TimeOnly(9, 0)) }, CancellationToken.None)).Valeur;

        Assert.Equal(StatutOperation.Interdit, (await Service().AnnulerPriseAsync(CarnetDeTest.AutreUserId, priseId, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Succes, (await Service().AnnulerPriseAsync(CarnetDeTest.UserId, priseId, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Introuvable, (await Service().AnnulerPriseAsync(CarnetDeTest.UserId, priseId, CancellationToken.None)).Statut);
    }

    [Fact]
    public async Task Modifier_UnTraitementDejaArreteSansDateDeFin_NeLeRelancePas()
    {
        // Ancienne donnée : arrêté avant que la date de fin existe. L'enregistrer pour y ajouter une fréquence ne le relance pas.
        var id = await Creer(Quotidien("Ancien"));
        var traitement = await _carnet.Context.Medicaments.SingleAsync(m => m.Id == id);
        traitement.TraitementEnCours = false;
        traitement.DateFinTraitement = null;
        await _carnet.Context.SaveChangesAsync();

        await Service().ModifierAsync(CarnetDeTest.UserId, id, Quotidien("Ancien"), CancellationToken.None);

        Assert.False((await _carnet.Context.Medicaments.AsNoTracking().SingleAsync(m => m.Id == id)).TraitementEnCours);
        var vue = (await Service().GetDuJourAsync(CarnetDeTest.UserId, Jour, CancellationToken.None)).Valeur!;
        Assert.Empty(vue.PrisesPrevues);
    }

    [Fact]
    public async Task Arreter_AujourdHui_GardeLesPrisesDuJourVisibles()
    {
        var id = await Creer(Quotidien("Diénogest", "08:00", "20:00"));

        await Service().ArreterAsync(CarnetDeTest.UserId, id, Jour, CancellationToken.None);

        var vue = (await Service().GetDuJourAsync(CarnetDeTest.UserId, Jour, CancellationToken.None)).Valeur!;
        Assert.Equal(2, vue.PrisesPrevues.Count);
        var demain = (await Service().GetDuJourAsync(CarnetDeTest.UserId, Jour.AddDays(1), CancellationToken.None)).Valeur!;
        Assert.Empty(demain.PrisesPrevues);
    }

    [Fact]
    public async Task Modifier_RemplaceFrequenceEtHoraires_EtArreteSiLaFinEstPassee()
    {
        var id = await Creer(Quotidien("Diénogest", "08:00", "20:00"));
        var modification = Quotidien("Diénogest 2 mg", "21:00");
        modification.Frequence = FrequencePrise.CertainsJours;
        modification.JoursSemaine = ["Lundi", "Mercredi"];
        modification.DateFin = Jour.AddDays(-1);

        Assert.Equal(StatutOperation.Succes, (await Service().ModifierAsync(CarnetDeTest.UserId, id, modification, CancellationToken.None)).Statut);

        var traitement = await _carnet.Context.Medicaments.Include(m => m.Horaires).SingleAsync(m => m.Id == id);
        Assert.Equal("Diénogest 2 mg", traitement.Nom);
        Assert.Equal(JoursSemaine.Lundi | JoursSemaine.Mercredi, traitement.JoursSemaine);
        Assert.Equal([new TimeOnly(21, 0)], traitement.Horaires.Select(h => h.Heure));
        Assert.False(traitement.TraitementEnCours);
    }

    [Fact]
    public async Task Saisies_Invalides_OuDUnAutreCarnet_Refusees()
    {
        var id = await Creer(Quotidien());
        var sansHoraire = Quotidien();
        sansHoraire.Horaires = [];

        Assert.Equal(StatutOperation.Invalide, (await Service().CreerAsync(CarnetDeTest.UserId, sansHoraire, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Invalide, (await Service().ModifierAsync(CarnetDeTest.UserId, id, sansHoraire, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Interdit, (await Service().ModifierAsync(CarnetDeTest.AutreUserId, id, Quotidien(), CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Interdit, (await Service().NoterPriseAsync(CarnetDeTest.AutreUserId, id, new PriseDto { Date = Jour.ToDateTime(TimeOnly.MinValue) }, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Interdit, (await Service().ArreterAsync(CarnetDeTest.AutreUserId, id, Jour, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Introuvable, (await Service().ModifierAsync(CarnetDeTest.UserId, 999, Quotidien(), CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Invalide, (await Service().NoterSeanceAsync(CarnetDeTest.UserId, id, new SeanceDto { Date = Jour.ToDateTime(TimeOnly.MinValue) }, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Invalide, (await Service().NoterPriseAsync(CarnetDeTest.UserId, id, new PriseDto { Statut = (StatutPrise)9, Date = Jour.ToDateTime(TimeOnly.MinValue) }, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.NonAuthentifie, (await Service().GetDuJourAsync("inconnue", Jour, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.NonAuthentifie, (await Service().CreerAsync("", Quotidien(), CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.NonAuthentifie, (await Service().AnnulerPriseAsync("inconnue", 1, CancellationToken.None)).Statut);
    }

    [Fact]
    public async Task Soin_SeNoteParSeance_PasParPrise()
    {
        var kine = await Creer(new TraitementDto { Nom = "Kiné", Type = TypeTraitement.NonMedicamenteux, Frequence = FrequencePrise.AuBesoin, DateDebut = Jour });

        Assert.Equal(StatutOperation.Invalide, (await Service().NoterPriseAsync(CarnetDeTest.UserId, kine, new PriseDto { Date = Jour.ToDateTime(TimeOnly.MinValue) }, CancellationToken.None)).Statut);
        var seance = await Service().NoterSeanceAsync(CarnetDeTest.UserId, kine, new SeanceDto { Date = Jour.ToDateTime(new TimeOnly(18, 0)), Duree = 45, Commentaire = "  dos  " }, CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, seance.Statut);
        Assert.Equal("dos", (await _carnet.Context.DonneesTraitementNonMedicamenteux.SingleAsync()).Commentaire);
    }

    [Fact]
    public async Task Controleur_TraduitLesResultats()
    {
        var controller = new TraitementsController(Service()) { ControllerContext = CarnetDeTest.ContexteAuthentifie() };

        Assert.IsType<OkObjectResult>(await controller.Creer(Quotidien(), CancellationToken.None));
        var id = await _carnet.Context.Medicaments.Select(m => m.Id).SingleAsync();
        Assert.IsType<OkObjectResult>(await controller.GetDuJour(Jour, CancellationToken.None));
        Assert.IsType<NoContentResult>(await controller.Modifier(id, Quotidien(), CancellationToken.None));
        var prise = Assert.IsType<OkObjectResult>(await controller.NoterPrise(id, new PriseDto { Date = Jour.ToDateTime(new TimeOnly(9, 0)) }, CancellationToken.None));
        var priseId = (int)prise.Value!.GetType().GetProperty("id")!.GetValue(prise.Value)!;
        Assert.IsType<NoContentResult>(await controller.AnnulerPrise(priseId, CancellationToken.None));
        Assert.IsType<BadRequestObjectResult>(await controller.NoterSeance(id, new SeanceDto { Date = Jour.ToDateTime(TimeOnly.MinValue) }, CancellationToken.None));
        Assert.IsType<NoContentResult>(await controller.Arreter(id, Jour, CancellationToken.None));
    }

    [Theory]
    [InlineData("0001-01-01")]
    [InlineData("2027-01-01")]
    public async Task NoterPriseEtSeance_DateAbsurdeOuFuture_EstRefusee(string date)
    {
        var id = await Creer(Quotidien());
        var kine = (await Service().CreerAsync(CarnetDeTest.UserId, new TraitementDto { Nom = "Kiné", Type = TypeTraitement.NonMedicamenteux, DateDebut = Jour.AddDays(-7) }, CancellationToken.None)).Valeur;
        var jour = DateTime.Parse(date);

        var prise = await Service().NoterPriseAsync(CarnetDeTest.UserId, id, new PriseDto { Statut = StatutPrise.Pris, Date = jour }, CancellationToken.None);
        var seance = await Service().NoterSeanceAsync(CarnetDeTest.UserId, kine, new SeanceDto { Date = jour }, CancellationToken.None);

        Assert.Equal(StatutOperation.Invalide, prise.Statut);
        Assert.Equal(StatutOperation.Invalide, seance.Statut);
        Assert.False(await _carnet.Context.DonneesMedicaments.AnyAsync(p => p.Date == jour));
        Assert.False(await _carnet.Context.DonneesTraitementNonMedicamenteux.AnyAsync(s => s.Date == jour));
    }

    public void Dispose() => _carnet.Dispose();
}
