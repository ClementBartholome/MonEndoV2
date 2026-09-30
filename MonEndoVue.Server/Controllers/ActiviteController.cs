using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Activite;

namespace MonEndoVue.Server.Controllers;

/// <summary>
/// Activités physiques de l'utilisatrice connectée (carnet déduit de la session, aucun identifiant de carnet en entrée).
/// Contrôleur HTTP uniquement : logique et contrôle d'accès dans <see cref="ActiviteService"/>.
/// </summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class ActiviteController(ActiviteService service) : ControllerBase
{
    /// <summary>Activités d'un mois (<c>?mois=2026-09-01</c>), de la plus récente à la plus ancienne.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMois([FromQuery] DateOnly mois, CancellationToken ct) =>
        this.VersReponse(await service.GetMoisAsync(User.GetCurrentUserId(), mois, ct), vue => Ok(vue));

    [HttpPost]
    public async Task<IActionResult> Creer(ActiviteDto dto, CancellationToken ct) =>
        this.VersReponse(await service.CreerAsync(User.GetCurrentUserId(), dto, ct), id => Ok(new { id }));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Modifier(int id, ActiviteDto dto, CancellationToken ct) =>
        this.VersReponse(await service.ModifierAsync(User.GetCurrentUserId(), id, dto, ct), NoContent);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Supprimer(int id, CancellationToken ct) =>
        this.VersReponse(await service.SupprimerAsync(User.GetCurrentUserId(), id, ct), NoContent);
}
