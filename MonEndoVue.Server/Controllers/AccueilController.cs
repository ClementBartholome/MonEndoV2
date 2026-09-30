using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Accueil;

namespace MonEndoVue.Server.Controllers;

/// <summary>Accueil « Aujourd'hui » de l'utilisatrice connectée (carnet déduit de la session, aucun identifiant en entrée).</summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class AccueilController(AccueilService service) : ControllerBase
{
    /// <summary>Le jour local de l'utilisatrice (<c>?jour=2026-09-28</c>), à deux jours près de la date du serveur.</summary>
    [HttpGet("aujourdhui")]
    public async Task<IActionResult> GetAujourdhui([FromQuery] DateOnly jour, CancellationToken ct) =>
        this.VersReponse(await service.GetAujourdhuiAsync(User.GetCurrentUserId(), jour, ct), aujourdhui => Ok(aujourdhui));
}
