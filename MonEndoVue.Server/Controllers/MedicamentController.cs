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
    public class MedicamentController(AppDbContext context, CarnetSanteService carnetSanteService) : ControllerBase
    {
        // GET: Medicament/ByCarnetSante/5
        [HttpGet("by-carnet-sante/{carnetSanteId:int}")]
        public async Task<ActionResult<IEnumerable<MedicamentViewModel>>> GetMedicaments(int carnetSanteId)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, carnetSanteId);
            if (securityCheck != null) return securityCheck;

            var medicaments = await context.Medicaments
                .Where(m => m.CarnetSanteId == carnetSanteId)
                .Select(m => new MedicamentViewModel
                {
                    Id = m.Id,
                    Nom = m.Nom,
                    Type = m.Type,
                    Posologie = m.Posologie,
                    TraitementEnCours = m.TraitementEnCours,
                    DateDebutTraitement = m.DateDebutTraitement,
                    DateFinTraitement = m.DateFinTraitement
                })
                .ToListAsync();

            return medicaments;
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
        public async Task<IActionResult> PutMedicament(int id, Medicament medicamentUpdate)
        {
            if (id != medicamentUpdate.Id)
            {
                return BadRequest();
            }

            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, medicamentUpdate.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            // Validation : La posologie est obligatoire pour les traitements médicamenteux
            if (medicamentUpdate.Type == TypeTraitement.Medicamenteux && string.IsNullOrWhiteSpace(medicamentUpdate.Posologie))
            {
                return BadRequest(new { message = "La posologie est obligatoire pour les traitements médicamenteux." });
            }

            // Récupérer l'entité existante de la base de données
            var existingMedicament = await context.Medicaments.FindAsync(id);
            if (existingMedicament == null)
            {
                return NotFound();
            }

            // Mettre à jour uniquement les propriétés modifiables
            existingMedicament.Nom = medicamentUpdate.Nom;
            existingMedicament.Type = medicamentUpdate.Type;
            existingMedicament.Posologie = medicamentUpdate.Posologie;
            existingMedicament.TraitementEnCours = medicamentUpdate.TraitementEnCours;
            existingMedicament.DateDebutTraitement = medicamentUpdate.DateDebutTraitement;
            existingMedicament.DateFinTraitement = medicamentUpdate.DateFinTraitement;

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

            // Validation : La posologie est obligatoire pour les traitements médicamenteux
            if (medicamentDto.Type == TypeTraitement.Medicamenteux && string.IsNullOrWhiteSpace(medicamentDto.Posologie))
            {
                return BadRequest(new { message = "La posologie est obligatoire pour les traitements médicamenteux." });
            }

            var medicament = new Medicament
            {
                CarnetSanteId = medicamentDto.CarnetSanteId,
                Nom = medicamentDto.Nom,
                Type = medicamentDto.Type,
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