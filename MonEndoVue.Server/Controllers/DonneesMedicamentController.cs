using Microsoft.AspNetCore.Authorization;
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
    public class DonneesMedicamentController(AppDbContext context, CarnetSanteService carnetSanteService)
        : ControllerBase
    {
        // GET: DonneesMedicament/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<DonneesMedicament>> GetDonneesMedicament(int id)
        {
            var donneesMedicament = await context.DonneesMedicaments.FindAsync(id);

            if (donneesMedicament == null)
            {
                return NotFound();
            }

            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, donneesMedicament.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            return donneesMedicament;
        }

        // GET: DonneesMedicament/ByMonth/5/2021
        [HttpGet("{carnetSanteId:int}/{month:int}/{year:int}")]
        public async Task<ActionResult<IEnumerable<DonneesMedicamentViewModel>>> GetDonneesMedicamentByMonth(
            int carnetSanteId, int month, int year)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, carnetSanteId);
            if (securityCheck != null) return securityCheck;
            
            var donneesMedicaments = await context.DonneesMedicaments
                .Include(dm => dm.Medicament)
                .Where(d => d.Date.Month == month && d.Date.Year == year && d.CarnetSanteId == carnetSanteId && d.Statut == StatutPrise.Pris)
                .ToArrayAsync();

            var donneesMedicamentViewModel = donneesMedicaments.Select(dm => new DonneesMedicamentViewModel
            {
                Id = dm.Id,
                NomMedicament = dm.Medicament.Nom,
                NombreComprimes = dm.NombreComprimes,
                Date = dm.Date,
                Commentaire = dm.Commentaire
            }).ToArray();

            return donneesMedicamentViewModel;
        }

        // PUT: DonneesMedicament/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutDonneesMedicament(int id, DonneesMedicament donneesMedicament)
        {
            if (id != donneesMedicament.Id)
            {
                return BadRequest();
            }

            var existing = await context.DonneesMedicaments.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, existing.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            if (!await MedicamentAppartientAuCarnet(donneesMedicament.MedicamentId, existing.CarnetSanteId))
            {
                return BadRequest(new { message = "Traitement introuvable." });
            }

            existing.MedicamentId = donneesMedicament.MedicamentId;
            existing.NombreComprimes = donneesMedicament.NombreComprimes;
            existing.Date = donneesMedicament.Date;
            existing.Commentaire = donneesMedicament.Commentaire;

            await context.SaveChangesAsync();
            carnetSanteService.InvalidateCache(existing.CarnetSanteId);

            return NoContent();
        }

        // POST: DonneesMedicament
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<DonneesMedicament>> PostDonneesMedicament(DonneesMedicament donneesMedicament)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, donneesMedicament.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            if (!await MedicamentAppartientAuCarnet(donneesMedicament.MedicamentId, donneesMedicament.CarnetSanteId))
            {
                return BadRequest(new { message = "Traitement introuvable." });
            }

            // Ne jamais créer de traitement via la navigation envoyée par le client
            donneesMedicament.Medicament = null;

            context.DonneesMedicaments.Add(donneesMedicament);
            await context.SaveChangesAsync();

            // Invalidate cache
            var carnetSanteId = donneesMedicament.CarnetSanteId;
            carnetSanteService.InvalidateCache(carnetSanteId);

            return CreatedAtAction("GetDonneesMedicament", new { id = donneesMedicament.Id }, donneesMedicament);
        }

        private Task<bool> MedicamentAppartientAuCarnet(int medicamentId, int carnetSanteId)
        {
            return context.Medicaments.AnyAsync(m => m.Id == medicamentId && m.CarnetSanteId == carnetSanteId);
        }

        // DELETE: DonneesMedicament/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteDonneesMedicament(int id)
        {
            var donneesMedicament = await context.DonneesMedicaments.FindAsync(id);
            if (donneesMedicament == null)
            {
                return NotFound();
            }
            
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, donneesMedicament.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            // Invalidate cache
            carnetSanteService.InvalidateCache(donneesMedicament.CarnetSanteId);

            context.DonneesMedicaments.Remove(donneesMedicament);
            await context.SaveChangesAsync();

            return NoContent();
        }
    }
}