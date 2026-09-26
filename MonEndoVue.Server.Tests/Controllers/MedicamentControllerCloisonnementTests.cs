using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Controllers;

public sealed class MedicamentControllerCloisonnementTests : IDisposable
{
    private readonly CarnetDeTest _carnet = new();
    private readonly MedicamentController _controller;

    public MedicamentControllerCloisonnementTests()
    {
        _controller = new MedicamentController(_carnet.Context, _carnet.CarnetSanteService)
        {
            ControllerContext = CarnetDeTest.ContexteAuthentifie(),
        };
    }

    private Medicament Existant(int carnetSanteId)
    {
        var medicament = new Medicament
        {
            CarnetSanteId = carnetSanteId,
            Nom = "Traitement initial",
            Type = TypeTraitement.Medicamenteux,
            Posologie = "1 par jour",
            TraitementEnCours = true,
            DateDebutTraitement = new DateTime(2026, 1, 1),
        };
        _carnet.Context.Medicaments.Add(medicament);
        _carnet.Context.SaveChanges();
        return medicament;
    }

    private static Medicament Modification(int id, int carnetSanteId) => new()
    {
        Id = id,
        CarnetSanteId = carnetSanteId,
        Nom = "Traitement modifié",
        Type = TypeTraitement.Medicamenteux,
        Posologie = "2 par jour",
        TraitementEnCours = false,
        DateDebutTraitement = new DateTime(2026, 2, 1),
        DateFinTraitement = new DateTime(2026, 3, 1),
    };

    [Fact]
    public async Task Get_Inexistant_RetourneNotFound()
    {
        var resultat = await _controller.GetMedicament(999);

        Assert.IsType<NotFoundResult>(resultat.Result);
    }

    [Fact]
    public async Task Get_TraitementDUneAutreUtilisatrice_RetourneForbid()
    {
        var medicament = Existant(CarnetDeTest.AutreCarnetSanteId);

        var resultat = await _controller.GetMedicament(medicament.Id);

        Assert.IsType<ForbidResult>(resultat.Result);
    }

    [Fact]
    public async Task Get_SonTraitement_RetourneLeTraitement()
    {
        var medicament = Existant(CarnetDeTest.CarnetSanteId);

        var resultat = await _controller.GetMedicament(medicament.Id);

        Assert.Same(medicament, resultat.Value);
    }

    [Fact]
    public async Task Put_IdDifferentDuCorps_RetourneBadRequest()
    {
        var resultat = await _controller.PutMedicament(1, Modification(2, CarnetDeTest.CarnetSanteId));

        Assert.IsType<BadRequestResult>(resultat);
    }

    [Fact]
    public async Task Put_Inexistant_RetourneNotFound()
    {
        var resultat = await _controller.PutMedicament(999, Modification(999, CarnetDeTest.CarnetSanteId));

        Assert.IsType<NotFoundResult>(resultat);
    }

    [Fact]
    public async Task Put_TraitementDUneAutreUtilisatrice_RetourneForbidSansModifier()
    {
        var medicament = Existant(CarnetDeTest.AutreCarnetSanteId);

        var resultat = await _controller.PutMedicament(medicament.Id, Modification(medicament.Id, CarnetDeTest.CarnetSanteId));

        Assert.IsType<ForbidResult>(resultat);
        Assert.Equal(CarnetDeTest.AutreCarnetSanteId, medicament.CarnetSanteId);
        Assert.Equal("Traitement initial", medicament.Nom);
    }

    [Fact]
    public async Task Put_MedicamenteuxSansPosologie_RetourneBadRequest()
    {
        var medicament = Existant(CarnetDeTest.CarnetSanteId);
        var modification = Modification(medicament.Id, CarnetDeTest.CarnetSanteId);
        modification.Posologie = " ";

        var resultat = await _controller.PutMedicament(medicament.Id, modification);

        Assert.IsType<BadRequestObjectResult>(resultat);
        Assert.Equal("1 par jour", medicament.Posologie);
    }

    [Fact]
    public async Task Put_SonTraitement_CopieLesChampsSansChangerDeCarnet()
    {
        var medicament = Existant(CarnetDeTest.CarnetSanteId);

        var resultat = await _controller.PutMedicament(medicament.Id, Modification(medicament.Id, CarnetDeTest.AutreCarnetSanteId));

        Assert.IsType<NoContentResult>(resultat);
        Assert.Equal(CarnetDeTest.CarnetSanteId, medicament.CarnetSanteId);
        Assert.Equal("Traitement modifié", medicament.Nom);
        Assert.Equal("2 par jour", medicament.Posologie);
        Assert.False(medicament.TraitementEnCours);
        Assert.Equal(new DateTime(2026, 2, 1), medicament.DateDebutTraitement);
        Assert.Equal(new DateTime(2026, 3, 1), medicament.DateFinTraitement);
    }

    public void Dispose() => _carnet.Dispose();
}
