using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Consentement;
using MonEndoVue.Server.Services.Export;

namespace MonEndoVue.Server.Controllers;

/// <summary>
/// Droits de l'utilisatrice sur ses données (RGPD) : export complet. Accessible sans consentement à jour :
/// le droit d'accès ne dépend pas de l'accord au traitement.
/// </summary>
[Route("[controller]")]
[ApiController]
[Authorize]
[SansConsentement]
public class DonneesPersonnellesController(ExportDonneesService exportDonnees) : ControllerBase
{
    /// <summary>Archive ZIP de toutes les données du carnet de la session (JSON lisible + photos).</summary>
    [HttpGet("export")]
    [EnableRateLimiting(PolitiquesDebit.Auth)]
    public async Task<IActionResult> Exporter(CancellationToken ct)
    {
        var resultat = await exportDonnees.PreparerAsync(User.GetCurrentUserId(), ct);
        return this.VersReponse(resultat, export => File(export.Archive, ExportDonneesService.TypeContenu, export.NomFichier));
    }
}
