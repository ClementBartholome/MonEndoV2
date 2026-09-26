using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Controllers;

public sealed class DonneesActivitePhysiqueControllerCloisonnementTests : IDisposable
{
    private readonly CarnetDeTest _carnet = new();
    private readonly DonneesActivitePhysiqueController _controller;

    public DonneesActivitePhysiqueControllerCloisonnementTests()
    {
        _controller = new DonneesActivitePhysiqueController(_carnet.Context, _carnet.CarnetSanteService)
        {
            ControllerContext = CarnetDeTest.ContexteAuthentifie(),
        };
    }

    private DonneesActivitePhysique Existante(int carnetSanteId)
    {
        var activite = new DonneesActivitePhysique
        {
            CarnetSanteId = carnetSanteId,
            TypeActivite = "Marche",
            Date = new DateTime(2026, 9, 1),
            Duree = 30,
            Intensite = 3,
        };
        _carnet.Context.DonneesActivitePhysique.Add(activite);
        _carnet.Context.SaveChanges();
        return activite;
    }

    private static DonneesActivitePhysique Modification(int id, int carnetSanteId) => new()
    {
        Id = id,
        CarnetSanteId = carnetSanteId,
        TypeActivite = "Yoga",
        Date = new DateTime(2026, 9, 2),
        Duree = 45,
        Intensite = 6,
        EffetDouleur = 2,
        Commentaire = "Modifié",
    };

    [Fact]
    public async Task Get_Inexistante_RetourneNotFound()
    {
        var resultat = await _controller.GetDonneesActivitePhysique(999);

        Assert.IsType<NotFoundResult>(resultat.Result);
    }

    [Fact]
    public async Task Get_ActiviteDUneAutreUtilisatrice_RetourneForbid()
    {
        var activite = Existante(CarnetDeTest.AutreCarnetSanteId);

        var resultat = await _controller.GetDonneesActivitePhysique(activite.Id);

        Assert.IsType<ForbidResult>(resultat.Result);
    }

    [Fact]
    public async Task Get_SonActivite_RetourneLActivite()
    {
        var activite = Existante(CarnetDeTest.CarnetSanteId);

        var resultat = await _controller.GetDonneesActivitePhysique(activite.Id);

        Assert.Same(activite, resultat.Value);
    }

    [Fact]
    public async Task Put_IdDifferentDuCorps_RetourneBadRequest()
    {
        var resultat = await _controller.PutDonneesActivitePhysique(1, Modification(2, CarnetDeTest.CarnetSanteId));

        Assert.IsType<BadRequestResult>(resultat);
    }

    [Fact]
    public async Task Put_Inexistante_RetourneNotFound()
    {
        var resultat = await _controller.PutDonneesActivitePhysique(999, Modification(999, CarnetDeTest.CarnetSanteId));

        Assert.IsType<NotFoundResult>(resultat);
    }

    [Fact]
    public async Task Put_ActiviteDUneAutreUtilisatrice_RetourneForbidSansModifier()
    {
        var activite = Existante(CarnetDeTest.AutreCarnetSanteId);

        var resultat = await _controller.PutDonneesActivitePhysique(activite.Id, Modification(activite.Id, CarnetDeTest.CarnetSanteId));

        Assert.IsType<ForbidResult>(resultat);
        Assert.Equal(CarnetDeTest.AutreCarnetSanteId, activite.CarnetSanteId);
        Assert.Equal("Marche", activite.TypeActivite);
    }

    [Fact]
    public async Task Put_SonActivite_CopieLesChampsSansChangerDeCarnet()
    {
        var activite = Existante(CarnetDeTest.CarnetSanteId);

        var resultat = await _controller.PutDonneesActivitePhysique(activite.Id, Modification(activite.Id, CarnetDeTest.AutreCarnetSanteId));

        Assert.IsType<NoContentResult>(resultat);
        Assert.Equal(CarnetDeTest.CarnetSanteId, activite.CarnetSanteId);
        Assert.Equal("Yoga", activite.TypeActivite);
        Assert.Equal(new DateTime(2026, 9, 2), activite.Date);
        Assert.Equal(45, activite.Duree);
        Assert.Equal(6, activite.Intensite);
        Assert.Equal(2, activite.EffetDouleur);
        Assert.Equal("Modifié", activite.Commentaire);
    }

    public void Dispose() => _carnet.Dispose();
}
