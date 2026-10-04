using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Cycle;

namespace MonEndoVue.Server.Controllers;

/// <summary>
/// Règles et cycles de l'utilisatrice connectée (carnet déduit de la session, aucun identifiant de carnet en entrée).
/// Contrôleur HTTP uniquement : logique et contrôle d'accès dans <see cref="CycleService"/>.
/// </summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class CycleController(CycleService service) : ControllerBase
{
    /// <summary>
    /// Jours de règles d'un mois (<c>?mois=2026-09-01</c>), cycle en cours au jour local (<c>&amp;jour=2026-09-15</c>) et
    /// les <c>cycles</c> derniers cycles terminés (6 par défaut, 120 au plus).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateOnly jour, [FromQuery] DateOnly mois, CancellationToken ct, [FromQuery] int cycles = CycleService.CyclesDeLaMoyenne) =>
        this.VersReponse(await service.GetAsync(User.GetCurrentUserId(), jour, mois, ct, cycles), vue => Ok(vue));

    [HttpPut("regles/{jour}")]
    public async Task<IActionResult> AjouterJour(DateOnly jour, CancellationToken ct) =>
        this.VersReponse(await service.AjouterJourAsync(User.GetCurrentUserId(), jour, ct), NoContent);

    /// <summary>Flux et caillots d'un jour de règles noté (les deux champs remplacent les précédents ; nul = non précisé).</summary>
    [HttpPut("regles/{jour}/details")]
    public async Task<IActionResult> EnregistrerDetails(DateOnly jour, [FromBody] DetailsJourReglesDto details, CancellationToken ct) =>
        this.VersReponse(await service.EnregistrerDetailsAsync(User.GetCurrentUserId(), jour, details, ct), NoContent);

    [HttpDelete("regles/{jour}")]
    public async Task<IActionResult> RetirerJour(DateOnly jour, CancellationToken ct) =>
        this.VersReponse(await service.RetirerJourAsync(User.GetCurrentUserId(), jour, ct), NoContent);
}
