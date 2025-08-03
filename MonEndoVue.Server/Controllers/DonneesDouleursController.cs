using System.Security.Claims;
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
    public class DonneesDouleursController(AppDbContext context, CarnetSanteService carnetSanteService)
        : ControllerBase
    {
        // GET: DonneesDouleurs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DonneesDouleur>> GetDonneesDouleur(int id)
        {
            var donneesDouleur = await context.DonneesDouleurs.FindAsync(id);
            
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, donneesDouleur?.CarnetSanteId ?? 0);
            if (securityCheck != null) return securityCheck;

            if (donneesDouleur == null)
            {
                return NotFound();
            }

            return donneesDouleur;
        }

        // GET: DonneesDouleurs/ByMonth/5/2021
        [HttpGet("{carnetSanteId}/{month}/{year}")]
        public async Task<ActionResult<IEnumerable<DonneesDouleur>>> GetDonneesDouleurByMonth(int carnetSanteId,
            int month, int year)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, carnetSanteId);
            if (securityCheck != null) return securityCheck;
            
            var donneesDouleurs = await context.DonneesDouleurs
                .Where(d => d.Date.Month == month && d.Date.Year == year && d.CarnetSanteId == carnetSanteId)
                .ToArrayAsync();
            return donneesDouleurs;
        }

        // POST: DonneesDouleurs
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<DonneesDouleur>> PostDonneesDouleur(DonneesDouleur donneesDouleur)
        {
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, donneesDouleur.CarnetSanteId);
            if (securityCheck != null) return securityCheck;
            
            context.DonneesDouleurs.Add(donneesDouleur);
            await context.SaveChangesAsync();

            var carnetSanteId = donneesDouleur.CarnetSanteId;
            carnetSanteService.InvalidateCache(carnetSanteId);

            return CreatedAtAction("GetDonneesDouleur", new { id = donneesDouleur.Id }, donneesDouleur);
        }

        // DELETE: DonneesDouleurs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDonneesDouleur(int id)
        {
            var donneesDouleur = await context.DonneesDouleurs.FindAsync(id);
            if (donneesDouleur == null)
            {
                return NotFound();
            }
            
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, donneesDouleur.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            var carnetSanteId = donneesDouleur.CarnetSanteId;
            carnetSanteService.InvalidateCache(carnetSanteId);

            context.DonneesDouleurs.Remove(donneesDouleur);
            await context.SaveChangesAsync();

            return NoContent();
        }
    }
}