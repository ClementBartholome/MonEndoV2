using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;

namespace MonEndoVue.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class DonneesActivitePhysiqueController(AppDbContext context, CarnetSanteService carnetSanteService)
        : ControllerBase
    {
        // GET: DonneesActivitePhysique/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DonneesActivitePhysique>> GetDonneesActivitePhysique(int id)
        {
            var donneesActivitePhysique = await context.DonneesActivitePhysique.FindAsync(id);
            
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, donneesActivitePhysique?.CarnetSanteId ?? 0);
            if (securityCheck != null) return securityCheck;

            if (donneesActivitePhysique == null)
            {
                return NotFound();
            }

            return donneesActivitePhysique;
        }

        // GET: DonneesActivitePhysique/ByMonth/5/2021
        [HttpGet("{carnetSanteId}/{month}/{year}")]
        public async Task<ActionResult<IEnumerable<DonneesActivitePhysique>>> GetDonneesActivitePhysiqueByMonth(
            int carnetSanteId, int month, int year)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, carnetSanteId);
            if (securityCheck != null) return securityCheck;
            
            var donneesActivitePhysique = await context.DonneesActivitePhysique
                .Where(d => d.Date.Month == month && d.Date.Year == year && d.CarnetSanteId == carnetSanteId)
                .ToArrayAsync();
            return donneesActivitePhysique;
        }

        // PUT: api/DonneesActivitePhysique/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDonneesActivitePhysique(int id,
            DonneesActivitePhysique donneesActivitePhysique)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, donneesActivitePhysique.CarnetSanteId);
            if (securityCheck != null) return securityCheck;
            
            if (id != donneesActivitePhysique.Id)
            {
                return BadRequest();
            }

            context.Entry(donneesActivitePhysique).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DonneesActivitePhysiqueExists(id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }

        // POST: api/DonneesActivitePhysique
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<DonneesActivitePhysique>> PostDonneesActivitePhysique(
            DonneesActivitePhysique donneesActivitePhysique)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, donneesActivitePhysique.CarnetSanteId);
            if (securityCheck != null) return securityCheck;
            
            context.DonneesActivitePhysique.Add(donneesActivitePhysique);
            await context.SaveChangesAsync();

            // Invalidate cache
            var carnetSanteId = donneesActivitePhysique.CarnetSanteId;
            carnetSanteService.InvalidateCache(carnetSanteId);

            return CreatedAtAction("GetDonneesActivitePhysique", new { id = donneesActivitePhysique.Id },
                donneesActivitePhysique);
        }

        // DELETE: DonneesActivitePhysique/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDonneesActivitePhysique(int id)
        {
            var donneesActivitePhysique = await context.DonneesActivitePhysique.FindAsync(id);
            if (donneesActivitePhysique == null)
            {
                return NotFound();
            }
            
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, donneesActivitePhysique.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            // Invalidate cache
            var carnetSanteId = donneesActivitePhysique.CarnetSanteId;
            carnetSanteService.InvalidateCache(carnetSanteId);

            context.DonneesActivitePhysique.Remove(donneesActivitePhysique);
            await context.SaveChangesAsync();


            return NoContent();
        }

        private bool DonneesActivitePhysiqueExists(int id)
        {
            return context.DonneesActivitePhysique.Any(e => e.Id == id);
        }
    }
}