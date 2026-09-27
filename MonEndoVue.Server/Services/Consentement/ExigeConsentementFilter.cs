using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MonEndoVue.Server.Services.Consentement;

/// <summary>
/// Filtre global : une utilisatrice connectée sans consentement à jour reçoit un 403
/// <c>{ code: "consentement-requis" }</c> sur tout endpoint qui n'est pas marqué <see cref="SansConsentementAttribute"/>.
/// Les requêtes anonymes passent : <c>[Authorize]</c> les refuse ensuite si besoin.
/// </summary>
public sealed class ExigeConsentementFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var utilisatrice = context.HttpContext.User;
        if (utilisatrice.Identity?.IsAuthenticated != true) return;
        if (context.ActionDescriptor.EndpointMetadata.OfType<SansConsentementAttribute>().Any()) return;
        if (PolitiqueConfidentialite.EstAJour(utilisatrice)) return;

        context.Result = new ObjectResult(new
        {
            code = PolitiqueConfidentialite.CodeConsentementRequis,
            message = "Ton accord pour l'utilisation de tes données de santé est nécessaire pour continuer.",
        })
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };
    }
}
