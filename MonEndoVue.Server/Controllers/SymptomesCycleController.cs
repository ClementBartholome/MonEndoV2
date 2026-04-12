using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using System.Globalization;

namespace MonEndoVue.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class SymptomesCycleController(
        AppDbContext context,
        CarnetSanteService carnetSanteService,
        AzureBlobStorageService azureBlobStorageService,
        ILogger<SymptomesCycleController> logger) : ControllerBase
    {
        private const long MaxPhotoSizeInBytes = 10 * 1024 * 1024; // 10 MB
        private static readonly HashSet<string> AllowedMimeTypes =
        [
            "image/jpeg",
            "image/png",
            "image/webp",
            "image/heic",
            "image/heif"
        ];

        private static readonly HashSet<string> AllowedExtensions =
        [".jpg", ".jpeg", ".png", ".webp", ".heic", ".heif"];

        // GET: SymptomesCycle/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<SymptomeCycle>> GetSymptomeCycle(int id)
        {
            var symptomeCycle = await context.SymptomesCycles.FindAsync(id);

            if (symptomeCycle == null)
            {
                return NotFound();
            }
            
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, symptomeCycle.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            return symptomeCycle;
        }
        
        // GET: SymptomesCycle/ByMonth/5/2021
        [HttpGet("{carnetSanteId:int}/{month:int}/{year:int}")]
        public async Task<ActionResult<IEnumerable<SymptomeCycle>>> GetSymptomesCycleByMonth(int carnetSanteId, int month, int year)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, carnetSanteId);
            if (securityCheck != null) return securityCheck;
            
            return await context.SymptomesCycles
                .Where(d => d.Date.Month == month && d.Date.Year == year && d.CarnetSanteId == carnetSanteId)
                .ToArrayAsync();
        }
        
        // POST: SymptomesCycle
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<SymptomeCycle>> PostSymptomeCycle([FromForm] SymptomeCycle symptomeCycle, [FromForm] IFormFile? photo)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, symptomeCycle.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            if (photo != null)
            {
                if (!IsPhotoValid(photo, out var validationError))
                {
                    return BadRequest(new { message = validationError });
                }

                var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
                var fileName = $"symptomes/{symptomeCycle.CarnetSanteId.ToString(CultureInfo.InvariantCulture)}/{Guid.NewGuid()}{extension}";
                symptomeCycle.PhotoUrl = await azureBlobStorageService.UploadFileAsync(photo, fileName);
            }

            context.SymptomesCycles.Add(symptomeCycle);
            await context.SaveChangesAsync();

            carnetSanteService.InvalidateCache(symptomeCycle.CarnetSanteId);

            return CreatedAtAction("GetSymptomeCycle", new { id = symptomeCycle.Id }, symptomeCycle);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSymptomeCycle(int id)
        {
            var symptomeCycle = await context.SymptomesCycles.FindAsync(id);
            if (symptomeCycle == null) return NotFound();

            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, symptomeCycle.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            // Supprimer la photo si elle existe
            if (!string.IsNullOrEmpty(symptomeCycle.PhotoUrl))
            {
                try
                {
                    await azureBlobStorageService.DeleteFileByUrlAsync(symptomeCycle.PhotoUrl);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Unable to delete symptom photo for symptom {SymptomeId}", symptomeCycle.Id);
                }
            }

            context.SymptomesCycles.Remove(symptomeCycle);
            await context.SaveChangesAsync();

            carnetSanteService.InvalidateCache(symptomeCycle.CarnetSanteId);

            return NoContent();
        }

        private static bool IsPhotoValid(IFormFile photo, out string error)
        {
            if (photo.Length == 0)
            {
                error = "Le fichier photo est vide.";
                return false;
            }

            if (photo.Length > MaxPhotoSizeInBytes)
            {
                error = "La photo dépasse la taille maximale autorisée (10 MB).";
                return false;
            }

            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                error = "Format de photo non supporté.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(photo.ContentType) && !AllowedMimeTypes.Contains(photo.ContentType.ToLowerInvariant()))
            {
                error = "Type MIME de la photo non supporté.";
                return false;
            }

            error = string.Empty;
            return true;
        }

    }
}