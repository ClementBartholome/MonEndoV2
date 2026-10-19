using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Frise;

namespace MonEndoVue.Server.Controllers;

/// <summary>Frise de l'utilisatrice connectée (carnet déduit de la session, aucun identifiant en entrée).</summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class FriseController(FriseService service) : ControllerBase
{
    /// <summary>Les <c>?jours=</c> jours (62 au plus) qui finissent au jour local <c>?au=</c>, à deux jours près de la date du serveur.</summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateOnly au, [FromQuery] int jours, CancellationToken ct) =>
        this.VersReponse(await service.GetAsync(User.GetCurrentUserId(), au, jours, ct), frise => Ok(frise));
}
