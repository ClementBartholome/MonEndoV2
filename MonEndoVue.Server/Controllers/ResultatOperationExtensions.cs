using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Services;

namespace MonEndoVue.Server.Controllers;

/// <summary>Traduction d'un statut métier en réponse HTTP, identique pour tous les contrôleurs.</summary>
public static class ResultatOperationExtensions
{
    public static IActionResult VersReponse(this ControllerBase controller, StatutOperation statut, string? message, Func<IActionResult> succes) =>
        statut switch
        {
            StatutOperation.Succes => succes(),
            StatutOperation.Invalide => controller.BadRequest(new { message }),
            StatutOperation.NonAuthentifie => controller.Unauthorized(),
            StatutOperation.Interdit => controller.Forbid(),
            StatutOperation.TropDeTentatives => controller.StatusCode(StatusCodes.Status429TooManyRequests, new { message }),
            StatutOperation.Indisponible => controller.StatusCode(StatusCodes.Status503ServiceUnavailable, new { message }),
            _ => controller.NotFound(),
        };

    public static IActionResult VersReponse(this ControllerBase controller, ResultatOperation resultat, Func<IActionResult> succes) =>
        controller.VersReponse(resultat.Statut, resultat.Message, succes);

    public static IActionResult VersReponse<T>(this ControllerBase controller, ResultatOperation<T> resultat, Func<T, IActionResult> succes) =>
        controller.VersReponse(resultat.Statut, resultat.Message, () => succes(resultat.Valeur!));
}
