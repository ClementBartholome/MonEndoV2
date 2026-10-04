using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class BilanQuotidienController(
        AppDbContext context, CarnetSanteService carnetSanteService, TimeProvider horloge) : ControllerBase
    {
        // GET: BilanQuotidien/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BilanQuotidien>> GetBilanQuotidien(int id)
        {
            var bilanQuotidien = await context.BilansQuotidiens.FindAsync(id);

            if (bilanQuotidien == null)
            {
                return NotFound();
            }

            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, bilanQuotidien.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            return bilanQuotidien;
        }
        
        // GET: BilanQuotidien/periode?du=2026-09-01&au=2026-09-30 (carnet déduit de la session)
        [HttpGet("periode")]
        public async Task<IActionResult> GetPeriode(
            [FromQuery, BindRequired] DateOnly du, [FromQuery, BindRequired] DateOnly au,
            [FromServices] HistoriqueBilansService historique, CancellationToken cancellationToken) =>
            this.VersReponse(
                await historique.GetPeriodeAsync(User.GetCurrentUserId(), du, au, cancellationToken),
                periode => Ok(periode));

        // PUT: BilanQuotidien/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBilanQuotidien(int id, BilanQuotidien bilanQuotidien)
        {
            var erreur = Valider(bilanQuotidien);
            if (erreur != null) return erreur;

            if (id != bilanQuotidien.Id)
            {
                return BadRequest();
            }

            var existing = await context.BilansQuotidiens.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, existing.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            // Contrôle seulement si le jour change : d'anciens bilans ont pu être enregistrés en double sur un même jour
            // (date envoyée à minuit UTC par l'ancien client) et doivent rester modifiables.
            if (existing.Date.Date != bilanQuotidien.Date.Date
                && await ExisteUnAutreBilanLeMemeJour(existing.CarnetSanteId, bilanQuotidien.Date, id))
            {
                return Conflict(new { message = BilanDejaSaisi });
            }

            existing.Date = bilanQuotidien.Date;
            existing.Mood = bilanQuotidien.Mood;
            existing.Emotions.Clear();
            existing.Emotions.AddRange(NouvellesEmotions(bilanQuotidien));
            existing.StressPro = bilanQuotidien.StressPro;
            existing.StressPerso = bilanQuotidien.StressPerso;
            existing.Fatigue = bilanQuotidien.Fatigue;
            existing.Pas = bilanQuotidien.Pas;
            existing.DouleurMoyenne = bilanQuotidien.DouleurMoyenne;
            existing.Hydratation = bilanQuotidien.Hydratation;
            existing.Gluten = bilanQuotidien.Gluten;
            existing.Lactose = bilanQuotidien.Lactose;
            existing.Grignotage = bilanQuotidien.Grignotage;
            existing.Commentaire = bilanQuotidien.Commentaire;
            existing.Selles = bilanQuotidien.Selles;
            existing.TypeBristol = bilanQuotidien.TypeBristol;
            existing.CrampesEstomac = bilanQuotidien.CrampesEstomac;
            existing.IntensiteCrampes = bilanQuotidien.IntensiteCrampes;
            existing.Ballonnements = bilanQuotidien.Ballonnements;
            existing.IntensiteBallonnements = bilanQuotidien.IntensiteBallonnements;
            BilanCategories.Copier(bilanQuotidien, existing);

            await context.SaveChangesAsync();
            carnetSanteService.InvalidateCache(existing.CarnetSanteId);

            return NoContent();
        }
        
        // POST: BilanQuotidien
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<BilanQuotidien>> PostBilanQuotidien(BilanQuotidien bilanQuotidien)
        {
            var erreur = Valider(bilanQuotidien);
            if (erreur != null) return erreur;

            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, bilanQuotidien.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            if (await ExisteUnAutreBilanLeMemeJour(bilanQuotidien.CarnetSanteId, bilanQuotidien.Date, idExclu: null))
            {
                return Conflict(new { message = BilanDejaSaisi });
            }

            bilanQuotidien.Id = 0;
            bilanQuotidien.Emotions = NouvellesEmotions(bilanQuotidien);
            context.BilansQuotidiens.Add(bilanQuotidien);
            await context.SaveChangesAsync();
            carnetSanteService.InvalidateCache(bilanQuotidien.CarnetSanteId);

            return CreatedAtAction("GetBilanQuotidien", new { id = bilanQuotidien.Id }, bilanQuotidien);
        }

        private const string BilanDejaSaisi = "Un bilan existe déjà pour ce jour : modifie-le plutôt que d'en créer un second.";

        private BadRequestObjectResult? Valider(BilanQuotidien bilan)
        {
            var erreur = BilanHumeurValidator.Valider(bilan).Erreur
                ?? BilanTransitValidator.Valider(bilan).Erreur
                ?? BilanCategoriesValidator.Valider(bilan).Erreur
                ?? BilanMesuresValidator.Valider(bilan).Erreur
                ?? BilanDateValidator.Valider(bilan.Date, horloge.GetUtcNow()).Erreur;
            return erreur == null ? null : BadRequest(new { message = erreur });
        }

        // Un seul bilan par jour et par carnet (idExclu : le bilan en cours de modification).
        private Task<bool> ExisteUnAutreBilanLeMemeJour(int carnetSanteId, DateTime date, int? idExclu) =>
            context.BilansQuotidiens.AnyAsync(b =>
                b.CarnetSanteId == carnetSanteId && b.Date.Date == date.Date && b.Id != idExclu);

        // Seul le choix des émotions vient du client : leurs identifiants éventuels sont ignorés.
        private static List<EmotionBilan> NouvellesEmotions(BilanQuotidien bilan) =>
            bilan.Emotions.Select(e => new EmotionBilan { Emotion = e.Emotion }).ToList();

        // DELETE: BilanQuotidien/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBilanQuotidien(int id)
        {
            var bilanQuotidien = await context.BilansQuotidiens.FindAsync(id);
            if (bilanQuotidien == null)
            {
                return NotFound();
            }
            
            var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, bilanQuotidien.CarnetSanteId);
            if (securityCheck != null) return securityCheck;

            context.BilansQuotidiens.Remove(bilanQuotidien);
            await context.SaveChangesAsync();

            return NoContent();
        }
    }

}