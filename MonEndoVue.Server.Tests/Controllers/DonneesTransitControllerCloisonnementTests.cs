using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Controllers;

public sealed class DonneesTransitControllerCloisonnementTests : IDisposable
{
    private readonly CarnetDeTest _carnet = new();
    private readonly DonneesTransitController _controller;

    public DonneesTransitControllerCloisonnementTests()
    {
        _controller = new DonneesTransitController(_carnet.Context, _carnet.CarnetSanteService)
        {
            ControllerContext = CarnetDeTest.ContexteAuthentifie(),
        };
    }

    private DonneesTransit Existant(int carnetSanteId)
    {
        var transit = new DonneesTransit
        {
            CarnetSanteId = carnetSanteId,
            Date = new DateTime(2026, 9, 1),
            TypeEvenement = "Constipation",
            Intensite = "Légère",
        };
        _carnet.Context.DonneesTransit.Add(transit);
        _carnet.Context.SaveChanges();
        return transit;
    }

    private static DonneesTransit Modification(int id, int carnetSanteId) => new()
    {
        Id = id,
        CarnetSanteId = carnetSanteId,
        Date = new DateTime(2026, 9, 2),
        TypeEvenement = "Diarrhée",
        Intensite = "Sévère",
        Saignement = true,
        Douleur = true,
        Commentaires = "Modifié",
    };

    [Fact]
    public async Task Put_IdDifferentDuCorps_RetourneBadRequest()
    {
        var resultat = await _controller.PutDonneesTransit(1, Modification(2, CarnetDeTest.CarnetSanteId));

        Assert.IsType<BadRequestResult>(resultat);
    }

    [Fact]
    public async Task Put_Inexistant_RetourneNotFound()
    {
        var resultat = await _controller.PutDonneesTransit(999, Modification(999, CarnetDeTest.CarnetSanteId));

        Assert.IsType<NotFoundResult>(resultat);
    }

    [Fact]
    public async Task Put_TransitDUneAutreUtilisatrice_RetourneForbidSansModifier()
    {
        var transit = Existant(CarnetDeTest.AutreCarnetSanteId);

        var resultat = await _controller.PutDonneesTransit(transit.Id, Modification(transit.Id, CarnetDeTest.CarnetSanteId));

        Assert.IsType<ForbidResult>(resultat);
        Assert.Equal(CarnetDeTest.AutreCarnetSanteId, transit.CarnetSanteId);
        Assert.Equal("Constipation", transit.TypeEvenement);
    }

    [Fact]
    public async Task Put_SonTransit_CopieLesChampsSansChangerDeCarnet()
    {
        var transit = Existant(CarnetDeTest.CarnetSanteId);

        var resultat = await _controller.PutDonneesTransit(transit.Id, Modification(transit.Id, CarnetDeTest.AutreCarnetSanteId));

        Assert.IsType<NoContentResult>(resultat);
        Assert.Equal(CarnetDeTest.CarnetSanteId, transit.CarnetSanteId);
        Assert.Equal(new DateTime(2026, 9, 2), transit.Date);
        Assert.Equal("Diarrhée", transit.TypeEvenement);
        Assert.Equal("Sévère", transit.Intensite);
        Assert.True(transit.Saignement);
        Assert.True(transit.Douleur);
        Assert.Equal("Modifié", transit.Commentaires);
    }

    public void Dispose() => _carnet.Dispose();
}
