using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;

namespace MonEndoVue.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class MedicamentController(AppDbContext context, CarnetSanteService carnetSanteService) : ControllerBase
    {
        // GET: Medicament/ByCarnetSante/5
        [HttpGet("by-carnet-sante/{carnetSanteId:int}")]
        public async Task<ActionResult<IEnumerable<Medicament>>> GetMedicaments(int carnetSanteId)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, carnetSanteId);
            if (securityCheck != null) return securityCheck;
            
            return await context.Medicaments.Where(m => m.CarnetSanteId == carnetSanteId).ToListAsync();
        }

        // GET: Medicament/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Medicament>> GetMedicament(int id)
        {
            var medicament = await context.Medicaments.FindAsync(id);
            
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, medicament?.CarnetSanteId ?? 0);
            if (securityCheck != null) return securityCheck;

            if (medicament == null)
            {
                return NotFound();
            }

            return medicament;
        }

        // PUT: Medicament/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutMedicament(int id, Medicament medicament)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, medicament.CarnetSanteId);
            if (securityCheck != null) return securityCheck;
            
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
        public async Task<ActionResult<Medicament>> PostMedicament(MedicamentDto medicamentDto)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, medicamentDto.CarnetSanteId);
            if (securityCheck != null) return securityCheck;
            
            var medicament = new Medicament
            {
                CarnetSanteId = medicamentDto.CarnetSanteId,
                Nom = medicamentDto.Nom,
                Posologie = medicamentDto.Posologie,
                DateDebutTraitement = medicamentDto.DateDebutTraitement,
                DateFinTraitement = medicamentDto.DateFinTraitement,
                TraitementEnCours = true
            };

            context.Medicaments.Add(medicament);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetMedicament", new { id = medicament.Id }, medicament);
        }

        // DELETE: Medicament/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteMedicament(int id)
        {
            var medicament = await context.Medicaments.FindAsync(id);
            if (medicament == null)
            {
                return NotFound();
            }
            
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, medicament.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

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