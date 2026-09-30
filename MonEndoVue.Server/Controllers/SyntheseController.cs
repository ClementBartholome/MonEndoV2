using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Export;

namespace MonEndoVue.Server.Controllers;

/// <summary>
/// Synthèse du suivi pour préparer un rendez-vous (carnet déduit de la session, aucun identifiant de carnet en entrée).
/// Contrôleur HTTP uniquement : logique et contrôle d'accès dans <see cref="SyntheseRendezVousService"/>.
/// </summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class SyntheseController(SyntheseRendezVousService service) : ControllerBase
{
    /// <summary>Synthèse du <c>?du=2026-06-15</c> au <c>&amp;au=2026-09-15</c> inclus (un an au plus).</summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateOnly du, [FromQuery] DateOnly au, CancellationToken ct) =>
        this.VersReponse(await service.GetAsync(User.GetCurrentUserId(), du, au, ct), vue => Ok(vue));
}
