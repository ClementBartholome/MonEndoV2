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
        // GET: DonneesTransit
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DonneesTransit>>> GetDonneesTransit()
        {
            return await context.DonneesTransit.ToListAsync();
        }
        
        // GET: DonneesTransit/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DonneesTransit>> GetDonneesTransit(int id)
        {
            var donneesTransit = await context.DonneesTransit.FindAsync(id);

            if (donneesTransit == null)
            {
                return NotFound();
            }

            return donneesTransit;
        }
        
        // GET: DonneesTransit/ByMonth/5/2021
        [HttpGet("{carnetSanteId}/{month}/{year}")]
        public async Task<ActionResult<IEnumerable<DonneesTransit>>> GetDonneesTransitByMonth(int carnetSanteId, int month, int year)
        {
            return await context.DonneesTransit
                .Where(d => d.Date.Month == month && d.Date.Year == year && d.CarnetSanteId == carnetSanteId)
                .ToArrayAsync();
        }
        
        // POST: DonneesTransit
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<DonneesTransit>> PostDonneesTransit(DonneesTransit donneesTransit)
        {
            context.DonneesTransit.Add(donneesTransit);
            await context.SaveChangesAsync();

            var carnetSanteId = donneesTransit.CarnetSanteId;
            carnetSanteService.InvalidateCache(carnetSanteId);

            return CreatedAtAction("GetDonneesTransit", new { id = donneesTransit.Id }, donneesTransit);
        }
        
        // PUT: DonneesTransit/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDonneesTransit(int id, DonneesTransit donneesTransit)
        {
            if (id != donneesTransit.Id)
            {
                return BadRequest();
            }

            context.Entry(donneesTransit).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DonneesTransitExists(id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }
        
        private bool DonneesTransitExists(int id)
        {
            return context.DonneesTransit.Any(e => e.Id == id);
        }
        
        // DELETE: DonneesTransit/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDonneesTransit(int id)
        {
            var donneesTransit = await context.DonneesTransit.FindAsync(id);
            if (donneesTransit == null)
            {
                return NotFound();
            }
            var carnetSanteId = donneesTransit.CarnetSanteId;
            carnetSanteService.InvalidateCache(carnetSanteId);

            context.DonneesTransit.Remove(donneesTransit);
            await context.SaveChangesAsync();
            

            return NoContent();
        }
    }
}