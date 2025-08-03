using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace MonEndoVue.Server.Services
{
    public static class ControllerSecurityExtensions
    {
        /// <summary>
        /// Vérifie que l'utilisateur connecté a accès au carnet de santé spécifié
        /// </summary>
        /// <param name="controller">Le controller appelant</param>
        /// <param name="carnetSanteService">Le service CarnetSante</param>
        /// <param name="carnetSanteId">L'ID du carnet à vérifier</param>
        /// <returns>null si autorisé, sinon ActionResult avec l'erreur appropriée</returns>
        public static async Task<ActionResult?> ValidateCarnetAccess(
            this ControllerBase controller, 
            CarnetSanteService carnetSanteService, 
            int carnetSanteId)
        {
            var currentUserId = (ClaimsPrincipal.Current ?? controller.User).GetCurrentUserId();
            
            if (string.IsNullOrEmpty(currentUserId))
                return controller.Unauthorized();
            
            try
            {
                var userCarnet = await carnetSanteService.GetCarnetSanteByUserId(currentUserId);
                
                return carnetSanteId != userCarnet.Id ? controller.Forbid("Accès non autorisé") : null; // Accès autorisé
            }
            catch
            {
                return controller.Unauthorized();
            }
        }
    }
}