using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class BilanQuotidienController(AppDbContext context) : ControllerBase
    {
        // GET: BilanQuotidien
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BilanQuotidien>>> GetBilansQuotidiens()
        {
            return await context.BilansQuotidiens.ToListAsync();
        }
        
        // GET: BilanQuotidien/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BilanQuotidien>> GetBilanQuotidien(int id)
        {
            var bilanQuotidien = await context.BilansQuotidiens.FindAsync(id);

            if (bilanQuotidien == null)
            {
                return NotFound();
            }

            return bilanQuotidien;
        }
        
        // GET: BilanQuotidien/ByMonth/5/2021
        [HttpGet("{month}/{year}")]
        public async Task<ActionResult<IEnumerable<BilanQuotidien>>> GetBilanQuotidienByMonth(int carnetSanteId, int month, int year)
        {
            var bilansQuotidiens = await context.BilansQuotidiens
                .Where(d => d.Date.Month == month && d.Date.Year == year && d.CarnetSanteId == carnetSanteId)
                .ToArrayAsync();

            return bilansQuotidiens;
        }
        
        // GET: BilanQuotidien/ByWeek/5/2021
        [HttpGet("by-week/{carnetSanteId}/{week}/{year}")]
        public async Task<ActionResult<IEnumerable<BilanQuotidien>>> GetBilanQuotidienByWeek(int carnetSanteId, int week, int year)
        {
            var firstDayOfYear = new DateTime(year, 1, 1);
            var startOfWeek = firstDayOfYear.AddDays((week - 1) * 7 - (int)firstDayOfYear.DayOfWeek + (int)DayOfWeek.Monday);
            var endOfWeek = startOfWeek.AddDays(7);

            var bilansQuotidiens = await context.BilansQuotidiens
                .Where(d => d.Date >= startOfWeek && d.Date < endOfWeek && d.CarnetSanteId == carnetSanteId)
                .ToArrayAsync();

            return bilansQuotidiens;
        }
        
        
        // PUT: BilanQuotidien/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBilanQuotidien(int id, BilanQuotidien bilanQuotidien)
        {
            if (id != bilanQuotidien.Id)
            {
                return BadRequest();
            }

            context.Entry(bilanQuotidien).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BilanQuotidienExists(id))
                {
                    return NotFound();
                }

                throw;
            }

            return NoContent();
        }
        
        // POST: BilanQuotidien
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<BilanQuotidien>> PostBilanQuotidien(BilanQuotidien bilanQuotidien)
        {
            context.BilansQuotidiens.Add(bilanQuotidien);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetBilanQuotidien", new { id = bilanQuotidien.Id }, bilanQuotidien);
        }

        private bool BilanQuotidienExists(int id)
        {
            return context.BilansQuotidiens.Any(e => e.Id == id);
        }
        
        // DELETE: BilanQuotidien/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBilanQuotidien(int id)
        {
            var bilanQuotidien = await context.BilansQuotidiens.FindAsync(id);
            if (bilanQuotidien == null)
            {
                return NotFound();
            }

            context.BilansQuotidiens.Remove(bilanQuotidien);
            await context.SaveChangesAsync();

            return NoContent();
        }
    }

}