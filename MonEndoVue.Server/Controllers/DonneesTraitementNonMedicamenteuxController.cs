using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class DonneesTraitementNonMedicamenteuxController(AppDbContext context, CarnetSanteService carnetSanteService)
        : ControllerBase
    {
        // GET: DonneesTraitementNonMedicamenteux/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<DonneesTraitementNonMedicamenteux>> GetDonneesTraitementNonMedicamenteux(int id)
        {
            var donnees = await context.DonneesTraitementNonMedicamenteux.FindAsync(id);

            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, donnees?.CarnetSanteId ?? 0);
            if (securityCheck != null) return securityCheck;

            if (donnees == null)
            {
                return NotFound();
            }

            return donnees;
        }

        // GET: DonneesTraitementNonMedicamenteux/ByMonth/5/2021
        [HttpGet("{carnetSanteId:int}/{month:int}/{year:int}")]
        public async Task<ActionResult<IEnumerable<DonneesTraitementNonMedicamenteuxViewModel>>> GetDonneesTraitementNonMedicamenteuxByMonth(
            int carnetSanteId, int month, int year)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, carnetSanteId);
            if (securityCheck != null) return securityCheck;

            var donnees = await context.DonneesTraitementNonMedicamenteux
                .Include(d => d.Medicament)
                .Where(d => d.Date.Month == month && d.Date.Year == year && d.CarnetSanteId == carnetSanteId)
                .ToArrayAsync();

            var viewModels = donnees.Select(d => new DonneesTraitementNonMedicamenteuxViewModel
            {
                Id = d.Id,
                NomTraitement = d.Medicament!.Nom,
                Duree = d.Duree,
                Date = d.Date,
                Commentaire = d.Commentaire
            }).ToArray();

            return viewModels;
        }

        // PUT: DonneesTraitementNonMedicamenteux/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutDonneesTraitementNonMedicamenteux(int id, DonneesTraitementNonMedicamenteux donnees)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, donnees.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            if (id != donnees.Id)
            {
                return BadRequest();
            }

            context.Entry(donnees).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DonneesTraitementNonMedicamenteuxExists(id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }

        // POST: DonneesTraitementNonMedicamenteux
        [HttpPost]
        public async Task<ActionResult<DonneesTraitementNonMedicamenteux>> PostDonneesTraitementNonMedicamenteux(DonneesTraitementNonMedicamenteuxDto dto)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, dto.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            var donnees = new DonneesTraitementNonMedicamenteux
            {
                CarnetSanteId = dto.CarnetSanteId,
                MedicamentId = dto.MedicamentId,
                Duree = dto.Duree,
                Date = dto.Date,
                Commentaire = dto.Commentaire
            };

            context.DonneesTraitementNonMedicamenteux.Add(donnees);
            await context.SaveChangesAsync();

            // Invalidation du cache
            carnetSanteService.InvalidateCache(dto.CarnetSanteId);

            return CreatedAtAction("GetDonneesTraitementNonMedicamenteux", new { id = donnees.Id }, donnees);
        }

        private bool DonneesTraitementNonMedicamenteuxExists(int id)
        {
            return context.DonneesTraitementNonMedicamenteux.Any(e => e.Id == id);
        }

        // DELETE: DonneesTraitementNonMedicamenteux/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteDonneesTraitementNonMedicamenteux(int id)
        {
            var donnees = await context.DonneesTraitementNonMedicamenteux.FindAsync(id);
            if (donnees == null)
            {
                return NotFound();
            }

            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, donnees.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            // Invalidation du cache
            carnetSanteService.InvalidateCache(donnees.CarnetSanteId);

            context.DonneesTraitementNonMedicamenteux.Remove(donnees);
            await context.SaveChangesAsync();

            return NoContent();
        }
    }
}
