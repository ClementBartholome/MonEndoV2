using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Cycle;

namespace MonEndoVue.Server.Controllers;

/// <summary>
/// Suivi de l'acné de l'utilisatrice connectée : épisodes et suivis photo (carnet déduit de la session).
/// Contrôleur HTTP uniquement : logique et contrôle d'accès dans <see cref="AcneService"/>. Les photos de suivi
/// s'enregistrent comme symptômes « Acné » (<c>SymptomesCycleController</c>).
/// </summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class AcneController(AcneService service) : ControllerBase
{
    /// <summary>
    /// Épisodes et suivis photo des <c>mois</c> derniers mois (7 par défaut), durée de l'épisode en cours comptée au jour
    /// local (<c>?jour=2026-09-15</c>).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateOnly jour, CancellationToken ct, [FromQuery] int mois = AcneService.MoisDePhotos) =>
        this.VersReponse(await service.GetAsync(User.GetCurrentUserId(), jour, ct, mois), vue => Ok(vue));

    /// <summary>Photo d'un suivi (<c>id</c> du suivi) : lue par le serveur, réservée à sa propriétaire, non partagée en cache.</summary>
    [HttpGet("photos/{symptomeId:int}")]
    public async Task<IActionResult> Photo(int symptomeId, CancellationToken ct)
    {
        var resultat = await service.PhotoAsync(User.GetCurrentUserId(), symptomeId, ct);
        if (resultat.Statut == StatutOperation.Succes)
            Response.Headers.CacheControl = "private, max-age=60";
        return this.VersReponse(resultat, photo => File(photo.Contenu, photo.TypeContenu));
    }

    [HttpPost("episodes")]
    public async Task<IActionResult> Creer(EpisodeAcneDto dto, CancellationToken ct) =>
        this.VersReponse(await service.CreerAsync(User.GetCurrentUserId(), dto, ct), id => Ok(new { id }));

    [HttpPut("episodes/{id:int}")]
    public async Task<IActionResult> Modifier(int id, EpisodeAcneDto dto, CancellationToken ct) =>
        this.VersReponse(await service.ModifierAsync(User.GetCurrentUserId(), id, dto, ct), NoContent);

    [HttpPost("episodes/{id:int}/fin")]
    public async Task<IActionResult> Terminer(int id, FinEpisodeAcneDto dto, CancellationToken ct) =>
        this.VersReponse(await service.TerminerAsync(User.GetCurrentUserId(), id, dto.Fin, ct), NoContent);

    [HttpDelete("episodes/{id:int}")]
    public async Task<IActionResult> Supprimer(int id, CancellationToken ct) =>
        this.VersReponse(await service.SupprimerAsync(User.GetCurrentUserId(), id, ct), NoContent);
}
