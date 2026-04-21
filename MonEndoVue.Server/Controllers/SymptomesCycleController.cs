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
            "image/jpg",
            "image/pjpeg",
            "image/png",
            "image/webp",
            "image/heic",
            "image/heic-sequence",
            "image/heif",
            "image/heif-sequence"
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
        public async Task<ActionResult<SymptomeCycle>> PostSymptomeCycle([FromForm] SymptomeCycle symptomeCycle, [FromForm] IFormFile? photo, [FromForm] string? photoSource)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, symptomeCycle.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            if (photo != null)
            {
                if (!IsPhotoValid(photo, out var validationError))
                {
                    logger.LogWarning(
                        "Photo validation failed for POST symptom. CarnetSanteId={CarnetSanteId}, PhotoSource={PhotoSource}, FileName={FileName}, ContentType={ContentType}, Length={Length}, UserAgent={UserAgent}, Error={Error}",
                        symptomeCycle.CarnetSanteId,
                        photoSource,
                        photo.FileName,
                        photo.ContentType,
                        photo.Length,
                        Request.Headers.UserAgent.ToString(),
                        validationError);
                    return BadRequest(new { message = validationError });
                }

                var extension = ResolveFileExtension(photo);
                var fileName = $"symptomes/{symptomeCycle.CarnetSanteId.ToString(CultureInfo.InvariantCulture)}/{Guid.NewGuid()}{extension}";
                symptomeCycle.PhotoUrl = await azureBlobStorageService.UploadFileAsync(photo, fileName);
                logger.LogInformation(
                    "Photo uploaded for POST symptom. CarnetSanteId={CarnetSanteId}, PhotoSource={PhotoSource}, FileName={FileName}, StoredAs={StoredAs}",
                    symptomeCycle.CarnetSanteId,
                    photoSource,
                    photo.FileName,
                    fileName);
            }

            context.SymptomesCycles.Add(symptomeCycle);
            await context.SaveChangesAsync();

            carnetSanteService.InvalidateCache(symptomeCycle.CarnetSanteId);

            return CreatedAtAction("GetSymptomeCycle", new { id = symptomeCycle.Id }, symptomeCycle);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutSymptomeCycle(int id, [FromForm] SymptomeCycle symptomeCycle, [FromForm] IFormFile? photo, [FromForm] string? photoSource)
        {
            if (id != symptomeCycle.Id)
            {
                return BadRequest();
            }

            var existingSymptome = await context.SymptomesCycles.FindAsync(id);
            if (existingSymptome == null)
            {
                return NotFound();
            }

            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, existingSymptome.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            existingSymptome.TypeSymptome = symptomeCycle.TypeSymptome;
            existingSymptome.Date = symptomeCycle.Date;
            existingSymptome.Intensite = symptomeCycle.Intensite;
            existingSymptome.Commentaire = symptomeCycle.Commentaire;

            if (photo != null)
            {
                if (!IsPhotoValid(photo, out var validationError))
                {
                    logger.LogWarning(
                        "Photo validation failed for PUT symptom. SymptomeId={SymptomeId}, PhotoSource={PhotoSource}, FileName={FileName}, ContentType={ContentType}, Length={Length}, UserAgent={UserAgent}, Error={Error}",
                        existingSymptome.Id,
                        photoSource,
                        photo.FileName,
                        photo.ContentType,
                        photo.Length,
                        Request.Headers.UserAgent.ToString(),
                        validationError);
                    return BadRequest(new { message = validationError });
                }

                var previousPhotoUrl = existingSymptome.PhotoUrl;
                var extension = ResolveFileExtension(photo);
                var fileName = $"symptomes/{existingSymptome.CarnetSanteId.ToString(CultureInfo.InvariantCulture)}/{Guid.NewGuid()}{extension}";
                existingSymptome.PhotoUrl = await azureBlobStorageService.UploadFileAsync(photo, fileName);
                logger.LogInformation(
                    "Photo uploaded for PUT symptom. SymptomeId={SymptomeId}, PhotoSource={PhotoSource}, FileName={FileName}, StoredAs={StoredAs}",
                    existingSymptome.Id,
                    photoSource,
                    photo.FileName,
                    fileName);

                if (!string.IsNullOrWhiteSpace(previousPhotoUrl))
                {
                    try
                    {
                        await azureBlobStorageService.DeleteFileByUrlAsync(previousPhotoUrl);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Unable to delete previous symptom photo for symptom {SymptomeId}", existingSymptome.Id);
                    }
                }
            }

            await context.SaveChangesAsync();
            carnetSanteService.InvalidateCache(existingSymptome.CarnetSanteId);

            return NoContent();
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
            var normalizedMimeType = NormalizeMimeType(photo.ContentType);

            var hasAllowedExtension = !string.IsNullOrWhiteSpace(extension) && AllowedExtensions.Contains(extension);
            var hasAllowedMimeType = !string.IsNullOrWhiteSpace(normalizedMimeType) && AllowedMimeTypes.Contains(normalizedMimeType);

            // iOS peut envoyer un MIME vide/inattendu ou un nom sans extension: accepter si l'un des deux est reconnu.
            if (!hasAllowedExtension && !hasAllowedMimeType)
            {
                error = "Format de photo non supporté.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static string ResolveFileExtension(IFormFile photo)
        {
            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (AllowedExtensions.Contains(extension))
            {
                return extension;
            }

            return NormalizeMimeType(photo.ContentType) switch
            {
                "image/jpeg" or "image/jpg" or "image/pjpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/heic" or "image/heic-sequence" => ".heic",
                "image/heif" or "image/heif-sequence" => ".heif",
                _ => ".jpg"
            };
        }

        private static string NormalizeMimeType(string? contentType)
        {
            return contentType?.Trim().ToLowerInvariant() ?? string.Empty;
        }

    }
}