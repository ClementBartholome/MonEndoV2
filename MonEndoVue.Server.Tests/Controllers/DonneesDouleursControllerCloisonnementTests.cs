using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Dto;
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

    private static DonneesDouleurDto Modification() => new()
    {
        TypeDouleur = "Lombaire",
        Intensite = 8,
        Date = new DateTime(2026, 9, 2),
        Commentaire = "Modifié",
    };

    [Fact]
    public async Task Put_Inexistante_RetourneNotFound()
    {
        var resultat = await _controller.PutDonneesDouleur(999, Modification(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(resultat);
    }

    [Fact]
    public async Task Put_DouleurDUneAutreUtilisatrice_RetourneForbidSansModifier()
    {
        var douleur = Existante(CarnetDeTest.AutreCarnetSanteId);

        var resultat = await _controller.PutDonneesDouleur(douleur.Id, Modification(), CancellationToken.None);

        Assert.IsType<ForbidResult>(resultat);
        Assert.Equal("Pelvienne", douleur.TypeDouleur);
        Assert.Equal(5, douleur.Intensite);
    }

    [Fact]
    public async Task Put_SaDouleur_CopieLesChampsSansChangerDeCarnet()
    {
        var douleur = Existante(CarnetDeTest.CarnetSanteId);

        var resultat = await _controller.PutDonneesDouleur(douleur.Id, Modification(), CancellationToken.None);

        Assert.IsType<NoContentResult>(resultat);
        Assert.Equal(CarnetDeTest.CarnetSanteId, douleur.CarnetSanteId);
        Assert.Equal("Lombaire", douleur.TypeDouleur);
        Assert.Equal(8, douleur.Intensite);
        Assert.Equal(new DateTime(2026, 9, 2), douleur.Date);
        Assert.Equal("Modifié", douleur.Commentaire);
    }

    public void Dispose() => _carnet.Dispose();
}
