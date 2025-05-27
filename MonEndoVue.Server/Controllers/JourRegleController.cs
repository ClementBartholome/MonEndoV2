using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;

namespace MonEndoVue.Server.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize]
public class JourRegleController(AppDbContext context, CarnetSanteService carnetSanteService, ILogger<JourRegleController> logger) : ControllerBase
{
    [HttpGet("ByMonth/{carnetSanteId}/{month}/{year}/")]
    public async Task<IEnumerable<JourRegle>> GetByMonth(int carnetSanteId, int month, int year)
    {
        return await context.JourRegles
            .Where(j => j.Date.Month == month && j.Date.Year == year && j.CarnetSanteId == carnetSanteId)
            .ToListAsync();
    }

    [HttpGet]
    public async Task<IEnumerable<JourRegle>> Get()
    {
        return await context.JourRegles.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<JourRegle>> Get(int id)
    {
        var jourRegle = await context.JourRegles.FirstOrDefaultAsync(a => a.Id == id);
        
        if (jourRegle == null)
        {
            return NotFound();
        }
        
        return jourRegle;
    }

    [HttpPost]
    public async Task<ActionResult<JourRegle>> Post(JourRegle jourRegle)
    {
        context.JourRegles.Add(jourRegle);
        await context.SaveChangesAsync();

        // Invalidate cache
        var carnetSanteId = jourRegle.CarnetSanteId;
        carnetSanteService.InvalidateCache(carnetSanteId);

        return CreatedAtAction("Get", new { id = jourRegle.Id }, jourRegle);
    }

    [HttpPut]
    public async Task<JourRegle> Put(JourRegle jourRegle)
    {
        context.Entry(jourRegle).State = EntityState.Modified;
        await context.SaveChangesAsync();
        return jourRegle;
    }

    [HttpDelete("{id}")]
    public async Task Delete(int id)
    {
        context.JourRegles.Remove(new JourRegle { Id = id });
        await context.SaveChangesAsync();
    }
}