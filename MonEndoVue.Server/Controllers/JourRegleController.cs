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
    [HttpGet("ByMonth/{carnetSanteId:int}/{month:int}/{year:int}/")]
    public async Task<ActionResult<List<JourRegle>>> GetByMonth(int carnetSanteId, int month, int year)
    {
        var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, carnetSanteId);
        if (securityCheck != null) return securityCheck;

        var result = await context.JourRegles
            .Where(j => j.Date.Month == month && j.Date.Year == year && j.CarnetSanteId == carnetSanteId)
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<JourRegle>> Get(int id)
    {
        var jourRegle = await context.JourRegles.FirstOrDefaultAsync(a => a.Id == id);
        
        var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, jourRegle?.CarnetSanteId ?? 0);
        if (securityCheck != null) return securityCheck;
        
        if (jourRegle == null)
        {
            return NotFound();
        }
        
        return jourRegle;
    }

    [HttpPost]
    public async Task<ActionResult<JourRegle>> Post(JourRegle jourRegle)
    {
        var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, jourRegle.CarnetSanteId);
        if (securityCheck != null) return securityCheck;
        
        context.JourRegles.Add(jourRegle);
        await context.SaveChangesAsync();

        // Invalidate cache
        var carnetSanteId = jourRegle.CarnetSanteId;
        carnetSanteService.InvalidateCache(carnetSanteId);

        return CreatedAtAction("Get", new { id = jourRegle.Id }, jourRegle);
    }

    [HttpPut]
    public async Task<ActionResult<JourRegle>> Put(JourRegle jourRegle)
    {
        var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, jourRegle.CarnetSanteId);
        if (securityCheck != null) return securityCheck;
        
        context.Entry(jourRegle).State = EntityState.Modified;
        await context.SaveChangesAsync();
        return jourRegle;
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var jourRegle = await context.JourRegles.FirstOrDefaultAsync(j => j.Id == id);
        if (jourRegle == null) return NotFound();

        var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, jourRegle.CarnetSanteId);
        if (securityCheck != null) return securityCheck;

        context.JourRegles.Remove(jourRegle);
        await context.SaveChangesAsync();

        carnetSanteService.InvalidateCache(jourRegle.CarnetSanteId);

        return NoContent();
    }
}