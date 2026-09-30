using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Consentement;
using MonEndoVue.Server.Services.SuppressionCompte;

namespace MonEndoVue.Server.Controllers;

/// <summary>
/// Droit à l'effacement (RGPD) : suppression définitive du compte, sous <c>DonneesPersonnelles/</c> comme l'export.
/// Accessible sans consentement à jour : retirer son accord ne doit jamais être bloqué.
/// </summary>
[Route("DonneesPersonnelles")]
[ApiController]
[Authorize]
[SansConsentement]
public class SuppressionCompteController(SuppressionCompteService suppressionCompte) : ControllerBase
{
    /// <summary>Supprime définitivement le compte et toutes ses données, après vérification du mot de passe ; ferme la session.</summary>
    [HttpPost("suppression-compte")]
    [EnableRateLimiting(PolitiquesDebit.Auth)]
    public async Task<IActionResult> SupprimerCompte([FromBody] SuppressionCompteDto confirmation, CancellationToken ct)
    {
        var resultat = await suppressionCompte.SupprimerAsync(User.GetCurrentUserId(), confirmation.Password, ct);
        return this.VersReponse(resultat, () =>
        {
            // Mêmes attributs que les cookies de session posés par AccountController.
            var options = new CookieOptions { Path = "/", HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict };
            Response.Cookies.Delete("accessToken", options);
            Response.Cookies.Delete("refreshToken", options);
            return NoContent();
        });
    }
}
