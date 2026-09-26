using Microsoft.AspNetCore.Authorization;
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
    public class DonneesTransitController(AppDbContext context, CarnetSanteService carnetSanteService) : ControllerBase
    {
       
        // GET: DonneesTransit/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<DonneesTransit>> GetDonneesTransit(int id)
        {
            var donneesTransit = await context.DonneesTransit.FindAsync(id);

            if (donneesTransit == null)
            {
                return NotFound();
            }
            
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, donneesTransit.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            return donneesTransit;
        }
        
        // GET: DonneesTransit/ByMonth/5/2021
        [HttpGet("{carnetSanteId:int}/{month:int}/{year:int}")]
        public async Task<ActionResult<IEnumerable<DonneesTransit>>> GetDonneesTransitByMonth(int carnetSanteId, int month, int year)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, carnetSanteId);
            if (securityCheck != null) return securityCheck;
            
            return await context.DonneesTransit
                .Where(d => d.Date.Month == month && d.Date.Year == year && d.CarnetSanteId == carnetSanteId)
                .ToArrayAsync();
        }
        
        // POST: DonneesTransit
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<DonneesTransit>> PostDonneesTransit(DonneesTransit donneesTransit)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, donneesTransit.CarnetSanteId);
            if (securityCheck != null) return securityCheck;
            
            context.DonneesTransit.Add(donneesTransit);
            await context.SaveChangesAsync();

            var carnetSanteId = donneesTransit.CarnetSanteId;
            carnetSanteService.InvalidateCache(carnetSanteId);

            return CreatedAtAction("GetDonneesTransit", new { id = donneesTransit.Id }, donneesTransit);
        }
        
        // PUT: DonneesTransit/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutDonneesTransit(int id, DonneesTransit donneesTransit)
        {
            if (id != donneesTransit.Id)
            {
                return BadRequest();
            }

            var existing = await context.DonneesTransit.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, existing.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            existing.Date = donneesTransit.Date;
            existing.TypeEvenement = donneesTransit.TypeEvenement;
            existing.Intensite = donneesTransit.Intensite;
            existing.Saignement = donneesTransit.Saignement;
            existing.Douleur = donneesTransit.Douleur;
            existing.Commentaires = donneesTransit.Commentaires;

            await context.SaveChangesAsync();
            carnetSanteService.InvalidateCache(existing.CarnetSanteId);

            return NoContent();
        }

        // DELETE: DonneesTransit/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteDonneesTransit(int id)
        {
            var donneesTransit = await context.DonneesTransit.FindAsync(id);
            if (donneesTransit == null)
            {
                return NotFound();
            }

            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, donneesTransit.CarnetSanteId);
            if (securityCheck != null) return securityCheck;
            
            carnetSanteService.InvalidateCache(donneesTransit.CarnetSanteId);

            context.DonneesTransit.Remove(donneesTransit);
            await context.SaveChangesAsync();
            

            return NoContent();
        }
    }
}