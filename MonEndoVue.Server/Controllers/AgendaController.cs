using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Agenda;

namespace MonEndoVue.Server.Controllers;

/// <summary>
/// Agenda Google de l'utilisatrice connectée (lecture seule). Le calendrier est déduit de la session par
/// <see cref="AgendaService"/> : 404 si l'utilisatrice n'a pas d'agenda.
/// </summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class AgendaController(AgendaService service) : ControllerBase
{
    [HttpGet("evenements")]
    public async Task<IActionResult> GetEvenements(
        [FromQuery] DateTimeOffset debut, [FromQuery] DateTimeOffset fin, CancellationToken cancellationToken) =>
        this.VersReponse(
            await service.GetEvenementsAsync(User.GetCurrentUserId(), debut, fin, cancellationToken),
            evenements => Ok(evenements));

    [HttpGet("prochains")]
    public async Task<IActionResult> GetProchains(CancellationToken cancellationToken) =>
        this.VersReponse(
            await service.GetProchainsAsync(User.GetCurrentUserId(), cancellationToken),
            evenements => Ok(evenements));
}
