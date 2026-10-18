using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Frise;
using MonEndoVue.Server.Tests.Support;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>Frise : douleur la plus forte du jour (bilan ou entrée), règles, jours sans rien, cloisonnement par carnet.</summary>
public sealed class FriseServiceTests : IDisposable
{
    private static readonly DateOnly Jour = new(2026, 9, 28);
    private static readonly DateTimeOffset Maintenant = new(2026, 9, 28, 9, 0, 0, TimeSpan.Zero);

    private readonly CarnetDeTest _carnet = new();

    private FriseService Service() => new(_carnet.Context, new HorlogeFixe(Maintenant));

    private static DateTime A(int decalageJours, int heure = 9) => Jour.AddDays(decalageJours).ToDateTime(new TimeOnly(heure, 0));

    private async Task<IReadOnlyList<JourFriseViewModel>> Frise(int jours = 7)
    {
        var resultat = await Service().GetAsync(CarnetDeTest.UserId, Jour, jours, CancellationToken.None);
        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        return resultat.Valeur!;
    }

    [Fact]
    public async Task Frise_UnJourParJour_DouleurLaPlusForteEtRegles()
    {
        var ctx = _carnet.Context;
        const int c = CarnetDeTest.CarnetSanteId;
        ctx.BilansQuotidiens.AddRange(
            new BilanQuotidien { CarnetSanteId = c, Date = Jour.AddDays(-1).ToDateTime(TimeOnly.MinValue), DouleurMoyenne = 3 },
            new BilanQuotidien { CarnetSanteId = c, Date = Jour.ToDateTime(TimeOnly.MinValue), DouleurMoyenne = 6 });
        ctx.DonneesDouleurs.AddRange(
            // La veille : une entrée à 7 dépasse le bilan à 3.
            new DonneesDouleur { CarnetSanteId = c, TypeDouleur = "Douleur pelvienne", Intensite = 7, Date = A(-1, 20) },
            new DonneesDouleur { CarnetSanteId = c, TypeDouleur = "Douleur lombaire", Intensite = 2, Date = A(-3) },
            // Hors période : ignorée.
            new DonneesDouleur { CarnetSanteId = c, TypeDouleur = "Douleur pelvienne", Intensite = 9, Date = A(-7) });
        ctx.JourRegles.AddRange(new JourRegle { CarnetSanteId = c, Date = A(-1) }, new JourRegle { CarnetSanteId = c, Date = A(0) });
        await ctx.SaveChangesAsync();

        var frise = await Frise();

        Assert.Equal(7, frise.Count);
        Assert.Equal("2026-09-22", frise[0].Jour);
        Assert.Equal("2026-09-28", frise[6].Jour);
        Assert.Equal([null, null, null, 2, null, 7, 6], frise.Select(j => j.Douleur));
        Assert.Equal([false, false, false, false, false, true, true], frise.Select(j => j.Regles));
    }

    [Fact]
    public async Task Frise_IgnoreLesDonneesDUnAutreCarnet()
    {
        const int autre = CarnetDeTest.AutreCarnetSanteId;
        _carnet.Context.BilansQuotidiens.Add(new BilanQuotidien { CarnetSanteId = autre, Date = Jour.ToDateTime(TimeOnly.MinValue), DouleurMoyenne = 8 });
        _carnet.Context.DonneesDouleurs.Add(new DonneesDouleur { CarnetSanteId = autre, TypeDouleur = "Douleur pelvienne", Intensite = 9, Date = A(0) });
        _carnet.Context.JourRegles.Add(new JourRegle { CarnetSanteId = autre, Date = A(0) });
        await _carnet.Context.SaveChangesAsync();

        var frise = await Frise();

        Assert.All(frise, j => Assert.Null(j.Douleur));
        Assert.All(frise, j => Assert.False(j.Regles));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(63)]
    public async Task Frise_NombreDeJoursHorsLimites_Invalide(int jours)
    {
        var resultat = await Service().GetAsync(CarnetDeTest.UserId, Jour, jours, CancellationToken.None);

        Assert.Equal(StatutOperation.Invalide, resultat.Statut);
    }

    [Fact]
    public async Task Frise_JourDeFinTropEloigne_Invalide()
    {
        var resultat = await Service().GetAsync(CarnetDeTest.UserId, Jour.AddDays(-10), 7, CancellationToken.None);

        Assert.Equal(StatutOperation.Invalide, resultat.Statut);
    }

    [Fact]
    public async Task Frise_UtilisatriceSansCarnet_NonAuthentifiee()
    {
        var resultat = await Service().GetAsync("inconnue", Jour, 7, CancellationToken.None);

        Assert.Equal(StatutOperation.NonAuthentifie, resultat.Statut);
    }

    [Fact]
    public async Task Controleur_RenvoieLaFrise()
    {
        var controller = new FriseController(Service()) { ControllerContext = CarnetDeTest.ContexteAuthentifie() };

        var resultat = await controller.Get(Jour, 30, CancellationToken.None);

        var frise = Assert.IsAssignableFrom<IReadOnlyList<JourFriseViewModel>>(Assert.IsType<OkObjectResult>(resultat).Value);
        Assert.Equal(30, frise.Count);
    }

    public void Dispose() => _carnet.Dispose();
}
