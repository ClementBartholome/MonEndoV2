using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.WebPush;

namespace MonEndoVue.Server.Controllers;

/// <summary>
/// Notifications Web Push de l'utilisatrice connectée : abonnements de ses appareils et rappel quotidien du bilan.
/// Le carnet est toujours déduit de la session : aucun identifiant de carnet n'est accepté en entrée.
/// </summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class NotificationsController(
    AppDbContext context,
    NotificationsPushService notifications,
    IOptions<WebPushOptions> webPushOptions,
    TimeProvider timeProvider) : ControllerBase
{
    private const string MessageNonConfigure = "Les notifications ne sont pas disponibles pour le moment.";

    [HttpGet("cle-publique")]
    public IActionResult GetClePublique()
    {
        var options = webPushOptions.Value;
        return options.EstConfiguree
            ? Ok(new { clePublique = options.PublicKey })
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = MessageNonConfigure });
    }

    [HttpPost("abonnements")]
    public async Task<IActionResult> Abonner(AbonnementPushDto dto, CancellationToken cancellationToken)
    {
        var carnetSanteId = await CarnetCourantAsync(cancellationToken);
        if (carnetSanteId is null) return Unauthorized();

        if (!Uri.TryCreate(dto.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps)
        {
            return BadRequest(new { message = "Abonnement aux notifications invalide." });
        }

        // Un appareil n'a qu'un abonnement : s'il était lié à un autre compte, il est réaffecté au compte connecté.
        var abonnement = await context.AbonnementsPush
            .FirstOrDefaultAsync(a => a.Endpoint == dto.Endpoint, cancellationToken);
        if (abonnement is null)
        {
            abonnement = new AbonnementPush { Endpoint = dto.Endpoint, CreeLe = timeProvider.GetUtcNow().UtcDateTime };
            context.AbonnementsPush.Add(abonnement);
        }

        abonnement.CarnetSanteId = carnetSanteId.Value;
        abonnement.P256dh = dto.P256dh;
        abonnement.Auth = dto.Auth;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("abonnements")]
    public async Task<IActionResult> Desabonner(DesabonnementPushDto dto, CancellationToken cancellationToken)
    {
        var carnetSanteId = await CarnetCourantAsync(cancellationToken);
        if (carnetSanteId is null) return Unauthorized();

        var abonnement = await context.AbonnementsPush.FirstOrDefaultAsync(
            a => a.Endpoint == dto.Endpoint && a.CarnetSanteId == carnetSanteId, cancellationToken);
        if (abonnement is not null)
        {
            context.AbonnementsPush.Remove(abonnement);
            await context.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<PreferenceRappelDto>> GetPreferences(CancellationToken cancellationToken)
    {
        var carnetSanteId = await CarnetCourantAsync(cancellationToken);
        if (carnetSanteId is null) return Unauthorized();

        var preference = await context.PreferencesRappel
            .FirstOrDefaultAsync(p => p.CarnetSanteId == carnetSanteId, cancellationToken)
            ?? new PreferenceRappel();

        return new PreferenceRappelDto
        {
            RappelActif = preference.RappelActif,
            HeureRappel = preference.HeureRappel.ToString("HH:mm", CultureInfo.InvariantCulture),
            FuseauHoraire = preference.FuseauHoraire,
        };
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> PutPreferences(PreferenceRappelDto dto, CancellationToken cancellationToken)
    {
        var carnetSanteId = await CarnetCourantAsync(cancellationToken);
        if (carnetSanteId is null) return Unauthorized();

        if (!TimeOnly.TryParseExact(dto.HeureRappel, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var heure))
        {
            return BadRequest(new { message = "L'heure du rappel doit être au format HH:mm." });
        }

        if (!NotificationsPushService.EstFuseauValide(dto.FuseauHoraire))
        {
            return BadRequest(new { message = "Fuseau horaire inconnu." });
        }

        var preference = await context.PreferencesRappel
            .FirstOrDefaultAsync(p => p.CarnetSanteId == carnetSanteId, cancellationToken);
        if (preference is null)
        {
            preference = new PreferenceRappel { CarnetSanteId = carnetSanteId.Value };
            context.PreferencesRappel.Add(preference);
        }

        preference.RappelActif = dto.RappelActif;
        preference.HeureRappel = heure;
        preference.FuseauHoraire = dto.FuseauHoraire;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("test")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> EnvoyerTest(CancellationToken cancellationToken)
    {
        var carnetSanteId = await CarnetCourantAsync(cancellationToken);
        if (carnetSanteId is null) return Unauthorized();

        if (!webPushOptions.Value.EstConfiguree)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = MessageNonConfigure });
        }

        var envoyes = await notifications.EnvoyerAuCarnetAsync(
            carnetSanteId.Value,
            new MessagePush("MonEndo", "Les notifications fonctionnent sur cet appareil.", "/parametres"),
            cancellationToken);

        return envoyes > 0
            ? Ok(new { envoyes })
            : BadRequest(new { message = "Aucun appareil n'a reçu la notification. Réactive les notifications sur cet appareil." });
    }

    private async Task<int?> CarnetCourantAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return null;

        return await context.CarnetSantes
            .Where(c => c.UserId == userId)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
