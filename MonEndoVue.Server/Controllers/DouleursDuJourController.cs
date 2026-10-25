using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Douleurs;

namespace MonEndoVue.Server.Controllers;

/// <summary>Douleurs d'un jour pour le bilan (zones et crises), carnet déduit de la session, aucun identifiant en entrée.</summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class DouleursDuJourController(DouleursDuJourService service) : ControllerBase
{
    // GET: DouleursDuJour/2026-10-25
    [HttpGet("{jour}")]
    public async Task<IActionResult> Get(DateOnly jour, CancellationToken ct) =>
        this.VersReponse(await service.GetAsync(User.GetCurrentUserId(), jour, ct), douleurs => Ok(douleurs));

    // PUT: DouleursDuJour/2026-10-25/zones
    [HttpPut("{jour}/zones")]
    public async Task<IActionResult> PutZones(DateOnly jour, ZonesDouleurBilanDto dto, CancellationToken ct) =>
        this.VersReponse(await service.EnregistrerZonesAsync(User.GetCurrentUserId(), jour, dto, ct), NoContent);
}
