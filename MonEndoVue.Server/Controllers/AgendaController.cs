using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Dto;
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

    /// <summary>Dernier rendez-vous commencé avant <c>?avant=</c> (liste de zéro ou un élément).</summary>
    [HttpGet("precedent")]
    public async Task<IActionResult> GetPrecedent([FromQuery] DateTimeOffset avant, CancellationToken cancellationToken) =>
        this.VersReponse(
            await service.GetPrecedentAsync(User.GetCurrentUserId(), avant, cancellationToken),
            evenements => Ok(evenements));

    /// <summary>Calendriers que l'utilisatrice peut choisir (liste de Google, via sa liaison).</summary>
    [HttpGet("calendriers")]
    public async Task<IActionResult> GetCalendriers(CancellationToken cancellationToken) =>
        this.VersReponse(
            await service.ListerCalendriersAsync(User.GetCurrentUserId(), cancellationToken),
            calendriers => Ok(calendriers));

    [HttpPut("calendrier")]
    public async Task<IActionResult> ChoisirCalendrier(CalendrierChoisiDto dto, CancellationToken cancellationToken) =>
        this.VersReponse(
            await service.ChoisirCalendrierAsync(User.GetCurrentUserId(), dto.Id, cancellationToken),
            NoContent);

    [HttpGet("prochains")]
    public async Task<IActionResult> GetProchains(CancellationToken cancellationToken) =>
        this.VersReponse(
            await service.GetProchainsAsync(User.GetCurrentUserId(), cancellationToken),
            evenements => Ok(evenements));
}
