using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public class CarnetSanteController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly CarnetSanteService _carnetSanteService;

        public CarnetSanteController(AppDbContext context, CarnetSanteService carnetSanteService)
        {
            _context = context;
            _carnetSanteService = carnetSanteService;
        }

        // PUT: api/CarnetSante/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCarnetSante(int id, CarnetSante carnetSante)
        {
            if (id != carnetSante.Id)
            {
                return BadRequest();
            }

            _context.Entry(carnetSante).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CarnetSanteExists(id))
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

        // POST: api/CarnetSante
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<CarnetSante>> PostCarnetSante(CarnetSante carnetSante)
        {
            _context.CarnetSantes.Add(carnetSante);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCarnetSante", new { id = carnetSante.Id }, carnetSante);
        }

        // DELETE: api/CarnetSante/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCarnetSante(int id)
        {
            var carnetSante = await _context.CarnetSantes.FindAsync(id);
            if (carnetSante == null)
            {
                return NotFound();
            }

            _context.CarnetSantes.Remove(carnetSante);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CarnetSanteExists(int id)
        {
            return _context.CarnetSantes.Any(e => e.Id == id);
        }

        [HttpGet("user/id/{userId}")]
        public async Task<CarnetSante> GetCarnetSanteId(string userId)
        {
            return await _carnetSanteService.GetCarnetSanteId(userId);
        }

        [HttpGet("user/name/{username}")]
        public async Task<CarnetViewModel> GetCarnetSanteByUsername(string username)
        {
            return await _carnetSanteService.GetCarnetSanteByUsername(username);
        }

        [HttpGet("{carnetSanteId}")]
        public async Task<CarnetViewModel> GetCarnetSanteById(int carnetSanteId)
        {
            return await _carnetSanteService.GetCarnetSanteById(carnetSanteId);
        }
        
        [HttpGet("{carnetSanteId}/{month}/{year}")]
        public async Task<CarnetViewModel> GetDonneesCarnetSanteByMonth(int carnetSanteId, int month, int year)
        {
            return await _carnetSanteService.GetDonneesCarnetSanteByMonth(carnetSanteId, month, year);
        }
        
        [HttpGet("last-entries/{carnetSanteId}")]
        public async Task<CarnetHomepageViewModel> GetLastEntries(int carnetSanteId)
        {
            return await _carnetSanteService.GetLastEntries(carnetSanteId);
        }
    }
}
