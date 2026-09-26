using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Tests.Support;

namespace MonEndoVue.Server.Tests.Controllers;

public sealed class JourRegleControllerCloisonnementTests : IDisposable
{
    private readonly CarnetDeTest _carnet = new();
    private readonly JourRegleController _controller;

    public JourRegleControllerCloisonnementTests()
    {
        _controller = new JourRegleController(_carnet.Context, _carnet.CarnetSanteService, NullLogger<JourRegleController>.Instance)
        {
            ControllerContext = CarnetDeTest.ContexteAuthentifie(),
        };
    }

    private JourRegle Existant(int carnetSanteId)
    {
        var jour = new JourRegle { CarnetSanteId = carnetSanteId, Date = new DateTime(2026, 9, 1) };
        _carnet.Context.JourRegles.Add(jour);
        _carnet.Context.SaveChanges();
        return jour;
    }

    [Fact]
    public async Task Get_Inexistant_RetourneNotFound()
    {
        var resultat = await _controller.Get(999);

        Assert.IsType<NotFoundResult>(resultat.Result);
    }

    [Fact]
    public async Task Get_JourDUneAutreUtilisatrice_RetourneForbid()
    {
        var jour = Existant(CarnetDeTest.AutreCarnetSanteId);

        var resultat = await _controller.Get(jour.Id);

        Assert.IsType<ForbidResult>(resultat.Result);
    }

    [Fact]
    public async Task Get_SonJour_RetourneLeJour()
    {
        var jour = Existant(CarnetDeTest.CarnetSanteId);

        var resultat = await _controller.Get(jour.Id);

        Assert.Same(jour, resultat.Value);
    }

    [Fact]
    public async Task Put_Inexistant_RetourneNotFound()
    {
        var resultat = await _controller.Put(new JourRegle { Id = 999, CarnetSanteId = CarnetDeTest.CarnetSanteId });

        Assert.IsType<NotFoundResult>(resultat.Result);
    }

    [Fact]
    public async Task Put_JourDUneAutreUtilisatrice_RetourneForbidSansModifier()
    {
        var jour = Existant(CarnetDeTest.AutreCarnetSanteId);

        var resultat = await _controller.Put(new JourRegle
        {
            Id = jour.Id,
            CarnetSanteId = CarnetDeTest.CarnetSanteId,
            Date = new DateTime(2026, 9, 5),
        });

        Assert.IsType<ForbidResult>(resultat.Result);
        Assert.Equal(CarnetDeTest.AutreCarnetSanteId, jour.CarnetSanteId);
        Assert.Equal(new DateTime(2026, 9, 1), jour.Date);
    }

    [Fact]
    public async Task Put_SonJour_ModifieLaDateSansChangerDeCarnet()
    {
        var jour = Existant(CarnetDeTest.CarnetSanteId);

        var resultat = await _controller.Put(new JourRegle
        {
            Id = jour.Id,
            CarnetSanteId = CarnetDeTest.AutreCarnetSanteId,
            Date = new DateTime(2026, 9, 5),
        });

        Assert.Same(jour, resultat.Value);
        Assert.Equal(CarnetDeTest.CarnetSanteId, jour.CarnetSanteId);
        Assert.Equal(new DateTime(2026, 9, 5), jour.Date);
    }

    public void Dispose() => _carnet.Dispose();
}
