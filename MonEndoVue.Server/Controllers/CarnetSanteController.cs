using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class CarnetSanteController(AppDbContext context, CarnetSanteService carnetSanteService)
        : ControllerBase
    {
        [HttpGet("user/id/{userId}")]
        public async Task<ActionResult<CarnetSante>> GetCarnetSanteByUserId(string userId)
        {
            var currentUserId = (ClaimsPrincipal.Current ?? User).GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            // Vérifier que l'utilisateur demande ses propres données
            if (userId != currentUserId)
                return Forbid("Accès non autorisé");

            try
            {
                var carnet = await carnetSanteService.GetCarnetSanteByUserId(userId);
                return Ok(carnet);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("user/name/{username}")]
        public async Task<ActionResult<CarnetViewModel>> GetCarnetSanteByUsername(string username)
        {
            var currentUserId = (ClaimsPrincipal.Current ?? User).GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            try
            {
                // Vérifier que l'utilisateur demande ses propres données
                var currentUser = await context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
                if (currentUser?.UserName != username)
                    return Forbid("Accès non autorisé");

                var carnet = await carnetSanteService.GetCarnetSanteByUsername(username);
                return Ok(carnet);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("{carnetSanteId}")]
        public async Task<ActionResult<CarnetViewModel>> GetCarnetSanteById(int carnetSanteId)
        {
            var userId = (ClaimsPrincipal.Current ?? User).GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var carnet = await carnetSanteService.GetCarnetSanteById(carnetSanteId, userId);
                return Ok(carnet);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid("Accès non autorisé à ce carnet de santé");
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("{carnetSanteId}/{month}/{year}")]
        public async Task<ActionResult<CarnetPdfExportViewModel>> GetDonneesCarnetSanteByMonth(int carnetSanteId, int month, int year)
        {
            var userId = (ClaimsPrincipal.Current ?? User).GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var carnet = await carnetSanteService.GetDonneesCarnetSanteByMonthForPdf(carnetSanteId, month, year, userId);
                return Ok(carnet);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid("Accès non autorisé à ce carnet de santé");
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("current/{month}/{year}")]
        public async Task<ActionResult<CarnetPdfExportViewModel>> GetCurrentUserDonneesCarnetSanteByMonth(int month, int year)
        {
            var userId = (ClaimsPrincipal.Current ?? User).GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                // Récupérer d'abord le carnet de l'utilisateur
                var carnet = await carnetSanteService.GetCarnetSanteByUserId(userId);
                var exportData = await carnetSanteService.GetDonneesCarnetSanteByMonthForPdf(carnet.Id, month, year, userId);
                return Ok(exportData);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("last-entries/{carnetSanteId}")]
        public async Task<ActionResult<CarnetHomepageViewModel>> GetLastEntries(int carnetSanteId)
        {
            var userId = (ClaimsPrincipal.Current ?? User).GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var carnet = await carnetSanteService.GetLastEntries(carnetSanteId, userId);
                return Ok(carnet);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid("Accès non autorisé à ce carnet de santé");
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("current/last-entries")]
        public async Task<ActionResult<CarnetHomepageViewModel>> GetCurrentUserLastEntries()
        {
            var userId = (ClaimsPrincipal.Current ?? User).GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var carnet = await carnetSanteService.GetLastEntriesByUserId(userId);
                return Ok(carnet);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("create")]
        public async Task<ActionResult> CreateCarnetForCurrentUser()
        {
            var userId = (ClaimsPrincipal.Current ?? User).GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                await carnetSanteService.CreateCarnetSante(userId);
                return Ok("Carnet de santé créé avec succès");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erreur lors de la création du carnet");
            }
        }
    }
}