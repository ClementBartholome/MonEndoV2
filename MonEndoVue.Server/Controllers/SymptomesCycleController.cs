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
        public async Task<ActionResult<SymptomeCycle>> PostSymptomeCycle(SymptomeCycle symptomeCycle)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, symptomeCycle.CarnetSanteId);
            if (securityCheck != null) return securityCheck;
            
            context.SymptomesCycles.Add(symptomeCycle);
            await context.SaveChangesAsync();

            var carnetSanteId = symptomeCycle.CarnetSanteId;
            carnetSanteService.InvalidateCache(carnetSanteId);

            return CreatedAtAction("GetSymptomeCycle", new { id = symptomeCycle.Id }, symptomeCycle);
        }
        
        // DELETE: SymptomesCycle/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteSymptomeCycle(int id)
        {
            var symptomeCycle = await context.SymptomesCycles.FindAsync(id);
            if (symptomeCycle == null)
            {
                return NotFound();
            }
            
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, symptomeCycle.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            context.SymptomesCycles.Remove(symptomeCycle);
            await context.SaveChangesAsync();

            var carnetSanteId = symptomeCycle.CarnetSanteId;
            carnetSanteService.InvalidateCache(carnetSanteId);

            return NoContent();
        }
    }
}