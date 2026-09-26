using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using MonEndoVue.Server.Controllers;
using MonEndoVue.Server.Services;

namespace MonEndoVue.Server.Tests.Controllers;

public class ResultatOperationExtensionsTests
{
    private sealed class ControllerDeTest : ControllerBase;

    private static readonly ControllerDeTest Controller = new();

    [Theory]
    [InlineData(StatutOperation.Succes, 204)]
    [InlineData(StatutOperation.Invalide, 400)]
    [InlineData(StatutOperation.NonAuthentifie, 401)]
    [InlineData(StatutOperation.Introuvable, 404)]
    [InlineData(StatutOperation.Indisponible, 503)]
    public void VersReponse_TraduitChaqueStatutEnCodeHttp(StatutOperation statut, int codeAttendu)
    {
        var reponse = Controller.VersReponse(ResultatOperation.Echec(statut, "message"), () => new NoContentResult());

        Assert.Equal(codeAttendu, Assert.IsAssignableFrom<IStatusCodeActionResult>(reponse).StatusCode);
    }

    [Fact]
    public void VersReponse_Interdit_RetourneForbid()
    {
        var reponse = Controller.VersReponse(ResultatOperation.Echec(StatutOperation.Interdit), () => new NoContentResult());

        Assert.IsType<ForbidResult>(reponse);
    }

    [Fact]
    public void VersReponse_SuccesAvecValeur_TransmetLaValeur()
    {
        var reponse = Controller.VersReponse(ResultatOperation<int>.Succes(3), valeur => new OkObjectResult(valeur));

        Assert.Equal(3, Assert.IsType<OkObjectResult>(reponse).Value);
    }
}
