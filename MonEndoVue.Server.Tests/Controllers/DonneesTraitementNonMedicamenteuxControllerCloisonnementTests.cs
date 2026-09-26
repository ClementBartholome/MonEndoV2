using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Controllers;

public sealed class DonneesTraitementNonMedicamenteuxControllerCloisonnementTests : IDisposable
{
    private readonly CarnetDeTest _carnet = new();
    private readonly DonneesTraitementNonMedicamenteuxController _controller;
    private readonly Medicament _sonTraitement;
    private readonly Medicament _autreTraitementDuCarnet;
    private readonly Medicament _traitementDeLAutre;

    public DonneesTraitementNonMedicamenteuxControllerCloisonnementTests()
    {
        _controller = new DonneesTraitementNonMedicamenteuxController(_carnet.Context, _carnet.CarnetSanteService)
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
            Nom = "Kiné",
            Type = TypeTraitement.NonMedicamenteux,
            DateDebutTraitement = new DateTime(2026, 1, 1),
        };
        _carnet.Context.Medicaments.Add(medicament);
        _carnet.Context.SaveChanges();
        return medicament;
    }

    private DonneesTraitementNonMedicamenteux Seance(int carnetSanteId, int medicamentId)
    {
        var seance = new DonneesTraitementNonMedicamenteux
        {
            CarnetSanteId = carnetSanteId,
            MedicamentId = medicamentId,
            Duree = 30,
            Date = new DateTime(2026, 9, 1),
        };
        _carnet.Context.DonneesTraitementNonMedicamenteux.Add(seance);
        _carnet.Context.SaveChanges();
        return seance;
    }

    private static DonneesTraitementNonMedicamenteux Modification(int id, int carnetSanteId, int medicamentId) => new()
    {
        Id = id,
        CarnetSanteId = carnetSanteId,
        MedicamentId = medicamentId,
        Duree = 60,
        Date = new DateTime(2026, 9, 2),
        Commentaire = "Modifié",
    };

    [Fact]
    public async Task Get_Inexistante_RetourneNotFound()
    {
        var resultat = await _controller.GetDonneesTraitementNonMedicamenteux(999);

        Assert.IsType<NotFoundResult>(resultat.Result);
    }

    [Fact]
    public async Task Get_SeanceDUneAutreUtilisatrice_RetourneForbid()
    {
        var seance = Seance(CarnetDeTest.AutreCarnetSanteId, _traitementDeLAutre.Id);

        var resultat = await _controller.GetDonneesTraitementNonMedicamenteux(seance.Id);

        Assert.IsType<ForbidResult>(resultat.Result);
    }

    [Fact]
    public async Task Get_SaSeance_RetourneLaSeance()
    {
        var seance = Seance(CarnetDeTest.CarnetSanteId, _sonTraitement.Id);

        var resultat = await _controller.GetDonneesTraitementNonMedicamenteux(seance.Id);

        Assert.Same(seance, resultat.Value);
    }

    [Fact]
    public async Task Post_TraitementDUneAutreUtilisatrice_RetourneBadRequestSansEnregistrer()
    {
        var resultat = await _controller.PostDonneesTraitementNonMedicamenteux(new DonneesTraitementNonMedicamenteuxDto
        {
            CarnetSanteId = CarnetDeTest.CarnetSanteId,
            MedicamentId = _traitementDeLAutre.Id,
            Date = new DateTime(2026, 9, 1),
        });

        Assert.IsType<BadRequestObjectResult>(resultat.Result);
        Assert.Empty(_carnet.Context.DonneesTraitementNonMedicamenteux);
    }

    [Fact]
    public async Task Post_SonTraitement_EnregistreLaSeance()
    {
        var resultat = await _controller.PostDonneesTraitementNonMedicamenteux(new DonneesTraitementNonMedicamenteuxDto
        {
            CarnetSanteId = CarnetDeTest.CarnetSanteId,
            MedicamentId = _sonTraitement.Id,
            Duree = 45,
            Date = new DateTime(2026, 9, 1),
        });

        Assert.IsType<CreatedAtActionResult>(resultat.Result);
        var enregistree = Assert.Single(_carnet.Context.DonneesTraitementNonMedicamenteux);
        Assert.Equal(_sonTraitement.Id, enregistree.MedicamentId);
    }

    [Fact]
    public async Task Put_IdDifferentDuCorps_RetourneBadRequest()
    {
        var resultat = await _controller.PutDonneesTraitementNonMedicamenteux(1,
            Modification(2, CarnetDeTest.CarnetSanteId, _sonTraitement.Id));

        Assert.IsType<BadRequestResult>(resultat);
    }

    [Fact]
    public async Task Put_Inexistante_RetourneNotFound()
    {
        var resultat = await _controller.PutDonneesTraitementNonMedicamenteux(999,
            Modification(999, CarnetDeTest.CarnetSanteId, _sonTraitement.Id));

        Assert.IsType<NotFoundResult>(resultat);
    }

    [Fact]
    public async Task Put_SeanceDUneAutreUtilisatrice_RetourneForbidSansModifier()
    {
        var seance = Seance(CarnetDeTest.AutreCarnetSanteId, _traitementDeLAutre.Id);

        var resultat = await _controller.PutDonneesTraitementNonMedicamenteux(seance.Id,
            Modification(seance.Id, CarnetDeTest.CarnetSanteId, _sonTraitement.Id));

        Assert.IsType<ForbidResult>(resultat);
        Assert.Equal(CarnetDeTest.AutreCarnetSanteId, seance.CarnetSanteId);
        Assert.Equal(30, seance.Duree);
    }

    [Fact]
    public async Task Put_VersUnTraitementDUneAutreUtilisatrice_RetourneBadRequestSansModifier()
    {
        var seance = Seance(CarnetDeTest.CarnetSanteId, _sonTraitement.Id);

        var resultat = await _controller.PutDonneesTraitementNonMedicamenteux(seance.Id,
            Modification(seance.Id, CarnetDeTest.CarnetSanteId, _traitementDeLAutre.Id));

        Assert.IsType<BadRequestObjectResult>(resultat);
        Assert.Equal(_sonTraitement.Id, seance.MedicamentId);
    }

    [Fact]
    public async Task Put_SaSeance_CopieLesChampsSansChangerDeCarnet()
    {
        var seance = Seance(CarnetDeTest.CarnetSanteId, _sonTraitement.Id);

        var resultat = await _controller.PutDonneesTraitementNonMedicamenteux(seance.Id,
            Modification(seance.Id, CarnetDeTest.AutreCarnetSanteId, _autreTraitementDuCarnet.Id));

        Assert.IsType<NoContentResult>(resultat);
        Assert.Equal(CarnetDeTest.CarnetSanteId, seance.CarnetSanteId);
        Assert.Equal(_autreTraitementDuCarnet.Id, seance.MedicamentId);
        Assert.Equal(60, seance.Duree);
        Assert.Equal(new DateTime(2026, 9, 2), seance.Date);
        Assert.Equal("Modifié", seance.Commentaire);
    }

    public void Dispose() => _carnet.Dispose();
}
