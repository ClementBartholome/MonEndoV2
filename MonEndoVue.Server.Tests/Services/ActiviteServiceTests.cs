using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Activite;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Services;

/// <summary>Activités de la session : mois, niveaux d'intensité, effet sur la douleur, validation et cloisonnement.</summary>
public sealed class ActiviteServiceTests : IDisposable
{
    private static readonly DateOnly Septembre = new(2026, 9, 1);
    private static readonly DateTimeOffset Maintenant = new(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly CarnetDeTest _carnet = new();

    private ActiviteService Service() => new(_carnet.Context, _carnet.CarnetSanteService, new HorlogeFixe(Maintenant));

    private static ActiviteDto Marche(DateTime? date = null) => new()
    {
        Type = " Marche ", Date = date ?? new DateTime(2026, 9, 14, 18, 30, 0), Duree = 30,
        Niveau = NiveauActivite.Moderee, Effet = EffetActivite.Soulagee, Commentaire = "  ",
    };

    [Fact]
    public async Task Creer_EnregistreLeNiveauEtLAncienneEchelle()
    {
        var resultat = await Service().CreerAsync(CarnetDeTest.UserId, Marche(), CancellationToken.None);

        Assert.Equal(StatutOperation.Succes, resultat.Statut);
        var activite = await _carnet.Context.DonneesActivitePhysique.SingleAsync();
        Assert.Equal(CarnetDeTest.CarnetSanteId, activite.CarnetSanteId);
        Assert.Equal("Marche", activite.TypeActivite);
        Assert.Equal(2, activite.NiveauIntensite);
        Assert.Equal(5, activite.Intensite);
        Assert.Equal(1, activite.EffetDouleur);
        Assert.Null(activite.Commentaire);
    }

    [Fact]
    public async Task GetMois_SeulementLeMoisEtLeCarnet_AnciennesActivitesConverties()
    {
        _carnet.Context.DonneesActivitePhysique.AddRange(
            new DonneesActivitePhysique { CarnetSanteId = CarnetDeTest.CarnetSanteId, TypeActivite = "Yoga", Date = new DateTime(2026, 9, 3, 8, 0, 0), Duree = 20, Intensite = 9 },
            new DonneesActivitePhysique { CarnetSanteId = CarnetDeTest.CarnetSanteId, TypeActivite = "Vélo", Date = new DateTime(2026, 8, 30), Duree = 45, Intensite = 5 },
            new DonneesActivitePhysique { CarnetSanteId = CarnetDeTest.AutreCarnetSanteId, TypeActivite = "Natation", Date = new DateTime(2026, 9, 5), Duree = 30, Intensite = 5 });
        await _carnet.Context.SaveChangesAsync();
        await Service().CreerAsync(CarnetDeTest.UserId, Marche(), CancellationToken.None);

        var vue = (await Service().GetMoisAsync(CarnetDeTest.UserId, Septembre, CancellationToken.None)).Valeur!;

        Assert.Equal(["Marche", "Yoga"], vue.Select(a => a.Type));
        Assert.Equal("2026-09-14T18:30:00", vue[0].Date);
        // Sans niveau (avant la migration), l'ancienne intensité 9 correspond à « soutenue ».
        Assert.Equal(NiveauActivite.Soutenue, vue[1].Niveau);
        Assert.Equal(EffetActivite.NonRenseigne, vue[1].Effet);
    }

    [Fact]
    public async Task Saisies_Invalides_Refusees()
    {
        var service = Service();

        Assert.Equal(StatutOperation.Invalide, (await service.CreerAsync(CarnetDeTest.UserId, Marche(new DateTime(2026, 9, 20)), CancellationToken.None)).Statut);
        var inconnue = Marche();
        inconnue.Niveau = (NiveauActivite)7;
        Assert.Equal(StatutOperation.Invalide, (await service.CreerAsync(CarnetDeTest.UserId, inconnue, CancellationToken.None)).Statut);
        var sansType = Marche();
        sansType.Type = " ";
        Assert.Equal(StatutOperation.Invalide, (await service.CreerAsync(CarnetDeTest.UserId, sansType, CancellationToken.None)).Statut);
        Assert.Empty(await _carnet.Context.DonneesActivitePhysique.ToListAsync());
    }

    [Fact]
    public async Task ModifierEtSupprimer_ActiviteDUnAutreCarnet_Interdit()
    {
        var autre = new DonneesActivitePhysique { CarnetSanteId = CarnetDeTest.AutreCarnetSanteId, TypeActivite = "Yoga", Date = new DateTime(2026, 9, 3), Duree = 20, Intensite = 2 };
        _carnet.Context.DonneesActivitePhysique.Add(autre);
        await _carnet.Context.SaveChangesAsync();
        var service = Service();

        Assert.Equal(StatutOperation.Interdit, (await service.ModifierAsync(CarnetDeTest.UserId, autre.Id, Marche(), CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Interdit, (await service.SupprimerAsync(CarnetDeTest.UserId, autre.Id, CancellationToken.None)).Statut);
        Assert.Equal(StatutOperation.Introuvable, (await service.SupprimerAsync(CarnetDeTest.UserId, 999, CancellationToken.None)).Statut);
        Assert.Equal("Yoga", (await _carnet.Context.DonneesActivitePhysique.SingleAsync()).TypeActivite);
    }

    [Fact]
    public async Task Modifier_PuisSupprimer()
    {
        var id = (await Service().CreerAsync(CarnetDeTest.UserId, Marche(), CancellationToken.None)).Valeur;
        var soutenue = Marche();
        soutenue.Niveau = NiveauActivite.Soutenue;
        soutenue.Effet = EffetActivite.PlusForte;

        Assert.Equal(StatutOperation.Succes, (await Service().ModifierAsync(CarnetDeTest.UserId, id, soutenue, CancellationToken.None)).Statut);
        var activite = await _carnet.Context.DonneesActivitePhysique.SingleAsync();
        Assert.Equal((3, 8, 3), (activite.NiveauIntensite, activite.Intensite, activite.EffetDouleur));

        Assert.Equal(StatutOperation.Succes, (await Service().SupprimerAsync(CarnetDeTest.UserId, id, CancellationToken.None)).Statut);
        Assert.Empty(await _carnet.Context.DonneesActivitePhysique.ToListAsync());
    }

    public void Dispose() => _carnet.Dispose();
}
