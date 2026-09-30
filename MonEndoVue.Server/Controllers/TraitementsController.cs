using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Traitements;

namespace MonEndoVue.Server.Controllers;

/// <summary>
/// Traitements de l'utilisatrice connectée (carnet déduit de la session, aucun identifiant de carnet en entrée).
/// Contrôleur HTTP uniquement : logique et contrôle d'accès dans <see cref="TraitementsService"/>.
/// </summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class TraitementsController(TraitementsService service) : ControllerBase
{
    /// <summary>Planning du jour local de l'utilisatrice (<c>?jour=2026-09-28</c>) et liste des traitements.</summary>
    [HttpGet("jour")]
    public async Task<IActionResult> GetDuJour([FromQuery] DateOnly jour, CancellationToken ct) =>
        this.VersReponse(await service.GetDuJourAsync(User.GetCurrentUserId(), jour, ct), vue => Ok(vue));

    [HttpPost]
    public async Task<IActionResult> Creer(TraitementDto dto, CancellationToken ct) =>
        this.VersReponse(await service.CreerAsync(User.GetCurrentUserId(), dto, ct), id => Ok(new { id }));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Modifier(int id, TraitementDto dto, CancellationToken ct) =>
        this.VersReponse(await service.ModifierAsync(User.GetCurrentUserId(), id, dto, ct), NoContent);

    [HttpPost("{id:int}/arret")]
    public async Task<IActionResult> Arreter(int id, [FromQuery] DateOnly jour, CancellationToken ct) =>
        this.VersReponse(await service.ArreterAsync(User.GetCurrentUserId(), id, jour, ct), NoContent);

    [HttpPost("{id:int}/prises")]
    public async Task<IActionResult> NoterPrise(int id, PriseDto dto, CancellationToken ct) =>
        this.VersReponse(await service.NoterPriseAsync(User.GetCurrentUserId(), id, dto, ct), priseId => Ok(new { id = priseId }));

    [HttpDelete("prises/{priseId:int}")]
    public async Task<IActionResult> AnnulerPrise(int priseId, CancellationToken ct) =>
        this.VersReponse(await service.AnnulerPriseAsync(User.GetCurrentUserId(), priseId, ct), NoContent);

    [HttpPost("{id:int}/seances")]
    public async Task<IActionResult> NoterSeance(int id, SeanceDto dto, CancellationToken ct) =>
        this.VersReponse(await service.NoterSeanceAsync(User.GetCurrentUserId(), id, dto, ct), seanceId => Ok(new { id = seanceId }));

    [HttpDelete("seances/{seanceId:int}")]
    public async Task<IActionResult> AnnulerSeance(int seanceId, CancellationToken ct) =>
        this.VersReponse(await service.AnnulerSeanceAsync(User.GetCurrentUserId(), seanceId, ct), NoContent);

    /// <summary>Historique d'un mois (<c>?mois=2026-09-01&amp;jour=2026-09-15</c>, jour local : prises prévues jusqu'à ce jour).</summary>
    [HttpGet("{id:int}/historique")]
    public async Task<IActionResult> Historique(int id, [FromQuery] DateOnly mois, [FromQuery] DateOnly jour,
        [FromServices] HistoriqueTraitementsService historique, CancellationToken ct) =>
        this.VersReponse(await historique.GetAsync(User.GetCurrentUserId(), id, mois, jour, ct), vue => Ok(vue));
}
