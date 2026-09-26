using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Controllers;

public class BilanQuotidienControllerCloisonnementTests : IDisposable
{
    private readonly CarnetDeTest _carnet = new();
    private readonly BilanQuotidienController _controller;

    public BilanQuotidienControllerCloisonnementTests()
    {
        _controller = new BilanQuotidienController(_carnet.Context, _carnet.CarnetSanteService)
        {
            ControllerContext = CarnetDeTest.ContexteAuthentifie(),
        };
    }

    private BilanQuotidien Existant(int carnetSanteId)
    {
        var bilan = new BilanQuotidien
        {
            CarnetSanteId = carnetSanteId,
            Date = new DateTime(2026, 9, 1),
            Mood = "Neutre",
            Fatigue = 3,
        };
        _carnet.Context.BilansQuotidiens.Add(bilan);
        _carnet.Context.SaveChanges();
        return bilan;
    }

    private static BilanQuotidien Modification(int id, int carnetSanteId) => new()
    {
        Id = id,
        CarnetSanteId = carnetSanteId,
        Date = new DateTime(2026, 9, 2),
        Mood = "Bien",
        StressPro = 1,
        StressPerso = 2,
        Fatigue = 7,
        Pas = 8000,
        DouleurMoyenne = 4,
        Hydratation = 1.5,
        Gluten = true,
        Lactose = true,
        Grignotage = true,
        Commentaire = "Modifié",
        Selles = true,
        TypeBristol = 4,
        CrampesEstomac = true,
        IntensiteCrampes = "Légère",
        Ballonnements = true,
        IntensiteBallonnements = "Forte",
    };

    [Fact]
    public async Task Get_Inexistant_RetourneNotFound()
    {
        var resultat = await _controller.GetBilanQuotidien(999);

        Assert.IsType<NotFoundResult>(resultat.Result);
    }

    [Fact]
    public async Task Get_BilanDUneAutreUtilisatrice_RetourneForbid()
    {
        var bilan = Existant(CarnetDeTest.AutreCarnetSanteId);

        var resultat = await _controller.GetBilanQuotidien(bilan.Id);

        Assert.IsType<ForbidResult>(resultat.Result);
    }

    [Fact]
    public async Task Get_SonBilan_RetourneLeBilan()
    {
        var bilan = Existant(CarnetDeTest.CarnetSanteId);

        var resultat = await _controller.GetBilanQuotidien(bilan.Id);

        Assert.Same(bilan, resultat.Value);
    }

    [Fact]
    public async Task Put_IdDifferentDuCorps_RetourneBadRequest()
    {
        var resultat = await _controller.PutBilanQuotidien(1, Modification(2, CarnetDeTest.CarnetSanteId));

        Assert.IsType<BadRequestResult>(resultat);
    }

    [Fact]
    public async Task Put_Inexistant_RetourneNotFound()
    {
        var resultat = await _controller.PutBilanQuotidien(999, Modification(999, CarnetDeTest.CarnetSanteId));

        Assert.IsType<NotFoundResult>(resultat);
    }

    [Fact]
    public async Task Put_BilanDUneAutreUtilisatrice_RetourneForbidSansModifier()
    {
        var bilan = Existant(CarnetDeTest.AutreCarnetSanteId);

        // Le corps annonce le carnet de l'utilisatrice connectée : seul le carnet en base doit compter.
        var resultat = await _controller.PutBilanQuotidien(bilan.Id, Modification(bilan.Id, CarnetDeTest.CarnetSanteId));

        Assert.IsType<ForbidResult>(resultat);
        Assert.Equal(CarnetDeTest.AutreCarnetSanteId, bilan.CarnetSanteId);
        Assert.Equal("Neutre", bilan.Mood);
    }

    [Fact]
    public async Task Put_SonBilan_CopieLesChampsSansChangerDeCarnet()
    {
        var bilan = Existant(CarnetDeTest.CarnetSanteId);

        var resultat = await _controller.PutBilanQuotidien(bilan.Id, Modification(bilan.Id, CarnetDeTest.AutreCarnetSanteId));

        Assert.IsType<NoContentResult>(resultat);
        Assert.Equal(CarnetDeTest.CarnetSanteId, bilan.CarnetSanteId);
        Assert.Equal(new DateTime(2026, 9, 2), bilan.Date);
        Assert.Equal("Bien", bilan.Mood);
        Assert.Equal(1, bilan.StressPro);
        Assert.Equal(2, bilan.StressPerso);
        Assert.Equal(7, bilan.Fatigue);
        Assert.Equal(8000, bilan.Pas);
        Assert.Equal(4, bilan.DouleurMoyenne);
        Assert.Equal(1.5, bilan.Hydratation);
        Assert.True(bilan.Gluten);
        Assert.True(bilan.Lactose);
        Assert.True(bilan.Grignotage);
        Assert.Equal("Modifié", bilan.Commentaire);
        Assert.True(bilan.Selles);
        Assert.Equal(4, bilan.TypeBristol);
        Assert.True(bilan.CrampesEstomac);
        Assert.Equal("Légère", bilan.IntensiteCrampes);
        Assert.True(bilan.Ballonnements);
        Assert.Equal("Forte", bilan.IntensiteBallonnements);
    }

    public void Dispose() => _carnet.Dispose();
}
