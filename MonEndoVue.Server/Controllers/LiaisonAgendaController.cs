using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Agenda;
using MonEndoVue.Server.Services.Consentement;

namespace MonEndoVue.Server.Controllers;

/// <summary>
/// Liaison de l'agenda Google de l'utilisatrice connectée. Départ, statut et déliaison sont authentifiés ; seul le
/// retour de Google (<see cref="Callback"/>) est anonyme, car le cookie de session (SameSite=Strict) n'accompagne pas
/// une redirection venue d'un autre site : l'utilisatrice y est identifiée par le cookie d'état chiffré.
/// </summary>
[Route("Agenda/liaison")]
[ApiController]
public class LiaisonAgendaController(LiaisonAgendaService service, TimeProvider horloge, IOptions<GoogleOAuthOptions>? options = null) : ControllerBase
{
    public const string CookieEtat = "monendo_liaison_agenda";
    private const string PageRetour = "/parametres";

    private string PageParametres => string.IsNullOrWhiteSpace(options?.Value.RetourApplication) ? PageRetour : options.Value.RetourApplication;

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetStatut(CancellationToken cancellationToken) =>
        Ok(await service.StatutAsync(User.GetCurrentUserId(), cancellationToken));

    /// <summary>Renvoie l'adresse d'autorisation Google (le client y redirige le navigateur) et pose le cookie d'état.</summary>
    [HttpPost("demarrer")]
    [Authorize]
    [EnableRateLimiting(PolitiquesDebit.Auth)]
    public async Task<IActionResult> Demarrer(CancellationToken cancellationToken) =>
        this.VersReponse(await service.DemarrerAsync(User.GetCurrentUserId(), cancellationToken), debut =>
        {
            Response.Cookies.Append(CookieEtat, debut.CookieEtat, OptionsCookie(EtatLiaison.Duree));
            return Ok(new { url = debut.UrlAutorisation });
        });

    /// <summary>Retour de Google : le code arrive dans l'URL. Toujours une redirection vers Paramètres.</summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    [SansConsentement]
    [EnableRateLimiting(PolitiquesDebit.Auth)]
    public async Task<IActionResult> Callback(
        [FromQuery] string? state, [FromQuery] string? code, [FromQuery] string? error, CancellationToken cancellationToken)
    {
        var resultat = await service.FinaliserAsync(Request.Cookies[CookieEtat], state, code, error, cancellationToken);
        // État à usage unique, quelle que soit l'issue.
        Response.Cookies.Delete(CookieEtat, OptionsCookie(null));
        return Redirect($"{PageParametres}?agenda={(resultat.Statut == StatutOperation.Succes ? "lie" : "echec")}");
    }

    [HttpDelete]
    [Authorize]
    public async Task<IActionResult> Delier(CancellationToken cancellationToken) =>
        this.VersReponse(await service.DelierAsync(User.GetCurrentUserId(), cancellationToken), NoContent);

    /// <summary>Lax : le cookie doit accompagner la redirection de Google ; Path restreint à ce contrôleur.</summary>
    private CookieOptions OptionsCookie(TimeSpan? duree) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/Agenda/liaison",
        MaxAge = duree,
        Expires = duree is null ? null : horloge.GetUtcNow() + duree.Value,
    };
}
