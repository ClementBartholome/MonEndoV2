using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.WebPush;

namespace MonEndoVue.Server.Controllers;

/// <summary>
/// Notifications Web Push de l'utilisatrice connectée. Contrôleur HTTP uniquement : la logique et le contrôle d'accès
/// (carnet déduit de la session) sont dans <see cref="NotificationsService"/>.
/// </summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class NotificationsController(NotificationsService service) : ControllerBase
{
    [HttpGet("cle-publique")]
    public IActionResult GetClePublique() =>
        this.VersReponse(service.ClePublique(), cle => Ok(new { clePublique = cle }));

    [HttpPost("abonnements")]
    public async Task<IActionResult> Abonner(AbonnementPushDto dto, CancellationToken cancellationToken) =>
        this.VersReponse(await service.AbonnerAsync(User.GetCurrentUserId(), dto, cancellationToken), NoContent);

    [HttpDelete("abonnements")]
    public async Task<IActionResult> Desabonner(DesabonnementPushDto dto, CancellationToken cancellationToken) =>
        this.VersReponse(await service.DesabonnerAsync(User.GetCurrentUserId(), dto.Endpoint, cancellationToken), NoContent);

    [HttpGet("rappels")]
    public async Task<IActionResult> GetRappels(CancellationToken cancellationToken) =>
        this.VersReponse(await service.GetRappelsAsync(User.GetCurrentUserId(), cancellationToken), rappels => Ok(rappels));

    [HttpPut("rappels/{type}")]
    public async Task<IActionResult> PutRappel(string type, RappelDto dto, CancellationToken cancellationToken) =>
        this.VersReponse(await service.PutRappelAsync(User.GetCurrentUserId(), type, dto, cancellationToken), NoContent);

    [HttpPost("test")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> EnvoyerTest(CancellationToken cancellationToken) =>
        this.VersReponse(await service.EnvoyerTestAsync(User.GetCurrentUserId(), cancellationToken), envoyes => Ok(new { envoyes }));
}
