using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Controllers;

public class DonneesDouleursControllerCloisonnementTests : IDisposable
{
    private readonly CarnetDeTest _carnet = new();
    private readonly DonneesDouleursController _controller;

    public DonneesDouleursControllerCloisonnementTests()
    {
        _controller = new DonneesDouleursController(_carnet.Context, _carnet.CarnetSanteService)
        {
            ControllerContext = CarnetDeTest.ContexteAuthentifie(),
        };
    }

    private DonneesDouleur Existante(int carnetSanteId)
    {
        var douleur = new DonneesDouleur
        {
            CarnetSanteId = carnetSanteId,
            TypeDouleur = "Pelvienne",
            Intensite = 5,
            Date = new DateTime(2026, 9, 1),
        };
        _carnet.Context.DonneesDouleurs.Add(douleur);
        _carnet.Context.SaveChanges();
        return douleur;
    }

    [Fact]
    public async Task Get_Inexistante_RetourneNotFound()
    {
        var resultat = await _controller.GetDonneesDouleur(999);

        Assert.IsType<NotFoundResult>(resultat.Result);
    }

    [Fact]
    public async Task Get_DouleurDUneAutreUtilisatrice_RetourneForbid()
    {
        var douleur = Existante(CarnetDeTest.AutreCarnetSanteId);

        var resultat = await _controller.GetDonneesDouleur(douleur.Id);

        Assert.IsType<ForbidResult>(resultat.Result);
    }

    [Fact]
    public async Task Get_SaDouleur_RetourneLaDouleur()
    {
        var douleur = Existante(CarnetDeTest.CarnetSanteId);

        var resultat = await _controller.GetDonneesDouleur(douleur.Id);

        Assert.Same(douleur, resultat.Value);
    }

    public void Dispose() => _carnet.Dispose();
}
