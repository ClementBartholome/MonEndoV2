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
    public class SymptomesCycleController(AppDbContext context, CarnetSanteService carnetSanteService) : ControllerBase
    {
        // GET: SymptomesCycle
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SymptomeCycle>>> GetSymptomesCycle()
        {
            return await context.SymptomesCycles.ToListAsync();
        }
        
        // GET: SymptomesCycle/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SymptomeCycle>> GetSymptomeCycle(int id)
        {
            var symptomeCycle = await context.SymptomesCycles.FindAsync(id);

            if (symptomeCycle == null)
            {
                return NotFound();
            }

            return symptomeCycle;
        }
        
        // GET: SymptomesCycle/ByMonth/5/2021
        [HttpGet("{carnetSanteId}/{month}/{year}")]
        public async Task<ActionResult<IEnumerable<SymptomeCycle>>> GetSymptomesCycleByMonth(int carnetSanteId, int month, int year)
        {
            return await context.SymptomesCycles
                .Where(d => d.Date.Month == month && d.Date.Year == year && d.CarnetSanteId == carnetSanteId)
                .ToArrayAsync();
        }
        
        // POST: SymptomesCycle
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<SymptomeCycle>> PostSymptomeCycle(SymptomeCycle symptomeCycle)
        {
            context.SymptomesCycles.Add(symptomeCycle);
            await context.SaveChangesAsync();

            var carnetSanteId = symptomeCycle.CarnetSanteId;
            carnetSanteService.InvalidateCache(carnetSanteId);

            return CreatedAtAction("GetSymptomeCycle", new { id = symptomeCycle.Id }, symptomeCycle);
        }
        
        // DELETE: SymptomesCycle/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSymptomeCycle(int id)
        {
            var symptomeCycle = await context.SymptomesCycles.FindAsync(id);
            if (symptomeCycle == null)
            {
                return NotFound();
            }

            context.SymptomesCycles.Remove(symptomeCycle);
            await context.SaveChangesAsync();

            var carnetSanteId = symptomeCycle.CarnetSanteId;
            carnetSanteService.InvalidateCache(carnetSanteId);

            return NoContent();
        }
    }
}