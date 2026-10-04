using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.Services.Photos;
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

            // L'identifiant et l'adresse de la photo viennent toujours du serveur, jamais du formulaire.
            symptomeCycle.Id = 0;
            symptomeCycle.PhotoUrl = null;

            if (photo != null)
            {
                if (!ValidationPhoto.Valider(photo, out var validationError, out var extensionPhoto))
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

                var extension = extensionPhoto!;
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
                if (!ValidationPhoto.Valider(photo, out var validationError, out var extensionPhoto))
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
                var extension = extensionPhoto!;
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
    }
}