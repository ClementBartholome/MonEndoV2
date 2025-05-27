using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class MedicamentController(AppDbContext context) : ControllerBase
    {
        // GET: Medicament/ByCarnetSante/5
        [HttpGet("by-carnet-sante/{carnetSanteId}")]
        public async Task<ActionResult<IEnumerable<Medicament>>> GetMedicaments(int carnetSanteId)
        {
            return await context.Medicaments.Where(m => m.CarnetSanteId == carnetSanteId).ToListAsync();
        }

        // GET: Medicament/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Medicament>> GetMedicament(int id)
        {
            var medicament = await context.Medicaments.FindAsync(id);

            if (medicament == null)
            {
                return NotFound();
            }

            return medicament;
        }

        // PUT: Medicament/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMedicament(int id, Medicament medicament)
        {
            if (id != medicament.Id)
            {
                return BadRequest();
            }

            context.Entry(medicament).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MedicamentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: Medicament
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Medicament>> PostMedicament(Medicament medicament)
        {
            medicament.TraitementEnCours = true;
            context.Medicaments.Add(medicament);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetMedicament", new { id = medicament.Id }, medicament);
        }

        // DELETE: Medicament/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMedicament(int id)
        {
            var medicament = await context.Medicaments.FindAsync(id);
            if (medicament == null)
            {
                return NotFound();
            }

            context.Medicaments.Remove(medicament);
            await context.SaveChangesAsync();

            return NoContent();
        }

        private bool MedicamentExists(int id)
        {
            return context.Medicaments.Any(e => e.Id == id);
        }
    }
}