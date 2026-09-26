using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Controllers;

public sealed class DonneesMedicamentControllerCloisonnementTests : IDisposable
{
    private readonly CarnetDeTest _carnet = new();
    private readonly DonneesMedicamentController _controller;
    private readonly Medicament _sonTraitement;
    private readonly Medicament _autreTraitementDuCarnet;
    private readonly Medicament _traitementDeLAutre;

    public DonneesMedicamentControllerCloisonnementTests()
    {
        _controller = new DonneesMedicamentController(_carnet.Context, _carnet.CarnetSanteService)
        {
            ControllerContext = CarnetDeTest.ContexteAuthentifie(),
        };
        _sonTraitement = Traitement(CarnetDeTest.CarnetSanteId);
        _autreTraitementDuCarnet = Traitement(CarnetDeTest.CarnetSanteId);
        _traitementDeLAutre = Traitement(CarnetDeTest.AutreCarnetSanteId);
    }

    private Medicament Traitement(int carnetSanteId)
    {
        var medicament = new Medicament
        {
            CarnetSanteId = carnetSanteId,
            Nom = "Traitement",
            Type = TypeTraitement.Medicamenteux,
            Posologie = "1 par jour",
            DateDebutTraitement = new DateTime(2026, 1, 1),
        };
        _carnet.Context.Medicaments.Add(medicament);
        _carnet.Context.SaveChanges();
        return medicament;
    }

    private DonneesMedicament Prise(int carnetSanteId, int medicamentId)
    {
        var prise = new DonneesMedicament
        {
            CarnetSanteId = carnetSanteId,
            MedicamentId = medicamentId,
            NombreComprimes = 1,
            Date = new DateTime(2026, 9, 1),
        };
        _carnet.Context.DonneesMedicaments.Add(prise);
        _carnet.Context.SaveChanges();
        return prise;
    }

    private static DonneesMedicament Modification(int id, int carnetSanteId, int medicamentId) => new()
    {
        Id = id,
        CarnetSanteId = carnetSanteId,
        MedicamentId = medicamentId,
        NombreComprimes = 3,
        Date = new DateTime(2026, 9, 2),
        Commentaire = "Modifié",
    };

    [Fact]
    public async Task Get_Inexistante_RetourneNotFound()
    {
        var resultat = await _controller.GetDonneesMedicament(999);

        Assert.IsType<NotFoundResult>(resultat.Result);
    }

    [Fact]
    public async Task Get_PriseDUneAutreUtilisatrice_RetourneForbid()
    {
        var prise = Prise(CarnetDeTest.AutreCarnetSanteId, _traitementDeLAutre.Id);

        var resultat = await _controller.GetDonneesMedicament(prise.Id);

        Assert.IsType<ForbidResult>(resultat.Result);
    }

    [Fact]
    public async Task Get_SaPrise_RetourneLaPrise()
    {
        var prise = Prise(CarnetDeTest.CarnetSanteId, _sonTraitement.Id);

        var resultat = await _controller.GetDonneesMedicament(prise.Id);

        Assert.Same(prise, resultat.Value);
    }

    [Fact]
    public async Task Post_TraitementDUneAutreUtilisatrice_RetourneBadRequestSansEnregistrer()
    {
        var resultat = await _controller.PostDonneesMedicament(
            Modification(0, CarnetDeTest.CarnetSanteId, _traitementDeLAutre.Id));

        Assert.IsType<BadRequestObjectResult>(resultat.Result);
        Assert.Empty(_carnet.Context.DonneesMedicaments);
    }

    [Fact]
    public async Task Post_AvecTraitementImbrique_NeCreePasDeTraitement()
    {
        var prise = Modification(0, CarnetDeTest.CarnetSanteId, _sonTraitement.Id);
        prise.Medicament = new Medicament
        {
            CarnetSanteId = CarnetDeTest.AutreCarnetSanteId,
            Nom = "Imbriqué",
            Type = TypeTraitement.NonMedicamenteux,
        };

        var resultat = await _controller.PostDonneesMedicament(prise);

        Assert.IsType<CreatedAtActionResult>(resultat.Result);
        Assert.Equal(3, _carnet.Context.Medicaments.Count());
        var enregistree = Assert.Single(_carnet.Context.DonneesMedicaments);
        Assert.Equal(_sonTraitement.Id, enregistree.MedicamentId);
    }

    [Fact]
    public async Task Put_IdDifferentDuCorps_RetourneBadRequest()
    {
        var resultat = await _controller.PutDonneesMedicament(1, Modification(2, CarnetDeTest.CarnetSanteId, _sonTraitement.Id));

        Assert.IsType<BadRequestResult>(resultat);
    }

    [Fact]
    public async Task Put_Inexistante_RetourneNotFound()
    {
        var resultat = await _controller.PutDonneesMedicament(999, Modification(999, CarnetDeTest.CarnetSanteId, _sonTraitement.Id));

        Assert.IsType<NotFoundResult>(resultat);
    }

    [Fact]
    public async Task Put_PriseDUneAutreUtilisatrice_RetourneForbidSansModifier()
    {
        var prise = Prise(CarnetDeTest.AutreCarnetSanteId, _traitementDeLAutre.Id);

        var resultat = await _controller.PutDonneesMedicament(prise.Id,
            Modification(prise.Id, CarnetDeTest.CarnetSanteId, _sonTraitement.Id));

        Assert.IsType<ForbidResult>(resultat);
        Assert.Equal(CarnetDeTest.AutreCarnetSanteId, prise.CarnetSanteId);
        Assert.Equal(_traitementDeLAutre.Id, prise.MedicamentId);
    }

    [Fact]
    public async Task Put_VersUnTraitementDUneAutreUtilisatrice_RetourneBadRequestSansModifier()
    {
        var prise = Prise(CarnetDeTest.CarnetSanteId, _sonTraitement.Id);

        var resultat = await _controller.PutDonneesMedicament(prise.Id,
            Modification(prise.Id, CarnetDeTest.CarnetSanteId, _traitementDeLAutre.Id));

        Assert.IsType<BadRequestObjectResult>(resultat);
        Assert.Equal(_sonTraitement.Id, prise.MedicamentId);
    }

    [Fact]
    public async Task Put_SaPrise_CopieLesChampsSansChangerDeCarnet()
    {
        var prise = Prise(CarnetDeTest.CarnetSanteId, _sonTraitement.Id);

        var resultat = await _controller.PutDonneesMedicament(prise.Id,
            Modification(prise.Id, CarnetDeTest.AutreCarnetSanteId, _autreTraitementDuCarnet.Id));

        Assert.IsType<NoContentResult>(resultat);
        Assert.Equal(CarnetDeTest.CarnetSanteId, prise.CarnetSanteId);
        Assert.Equal(_autreTraitementDuCarnet.Id, prise.MedicamentId);
        Assert.Equal(3, prise.NombreComprimes);
        Assert.Equal(new DateTime(2026, 9, 2), prise.Date);
        Assert.Equal("Modifié", prise.Commentaire);
    }

    public void Dispose() => _carnet.Dispose();
}
