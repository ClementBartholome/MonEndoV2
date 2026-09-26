# Gabarits API MonEndo

Remplacer `Xxx` par le nom de l'entité (`DonneesHydratation`…). Namespaces file-scoped, primary constructors.

## DTO d'entrée — `Dto/DonneesXxxDto.cs`
```csharp
namespace MonEndoVue.Server.Dto;

public class DonneesXxxDto
{
    public int CarnetSanteId { get; set; }          // utilisé uniquement à la création
    public DateTime Date { get; set; }
    public int Intensite { get; set; }
    public string? Commentaire { get; set; }
}
```

## ViewModel de sortie — `ViewModels/DonneesXxxViewModel.cs`
```csharp
namespace MonEndoVue.Server.ViewModels;

public class DonneesXxxViewModel
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int Intensite { get; set; }
    public string? Commentaire { get; set; }
}
```

## Contrôleur — `Controllers/DonneesXxxController.cs`
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize]
public class DonneesXxxController(
    AppDbContext context,
    CarnetSanteService carnetSanteService,
    ILogger<DonneesXxxController> logger) : ControllerBase
{
    [HttpGet("by-month/{carnetSanteId:int}/{month:int}/{year:int}")]
    public async Task<ActionResult<List<DonneesXxxViewModel>>> GetByMonth(
        int carnetSanteId, int month, int year, CancellationToken ct)
    {
        var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, carnetSanteId);
        if (securityCheck != null) return securityCheck;

        return await context.DonneesXxx
            .Where(x => x.CarnetSanteId == carnetSanteId && x.Date.Month == month && x.Date.Year == year)
            .OrderByDescending(x => x.Date)
            .Select(x => new DonneesXxxViewModel { Id = x.Id, Date = x.Date, Intensite = x.Intensite, Commentaire = x.Commentaire })
            .ToListAsync(ct);
    }

    [HttpPost]
    public async Task<ActionResult<DonneesXxxViewModel>> Post(DonneesXxxDto dto, CancellationToken ct)
    {
        var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, dto.CarnetSanteId);
        if (securityCheck != null) return securityCheck;

        var entity = new DonneesXxx
        {
            CarnetSanteId = dto.CarnetSanteId,
            Date = dto.Date,
            Intensite = dto.Intensite,
            Commentaire = dto.Commentaire,
        };
        context.DonneesXxx.Add(entity);
        await context.SaveChangesAsync(ct);
        carnetSanteService.InvalidateCache(entity.CarnetSanteId);

        logger.LogInformation("DonneesXxx created. Id={Id}", entity.Id);
        return CreatedAtAction(nameof(GetByMonth),
            new { carnetSanteId = entity.CarnetSanteId, month = entity.Date.Month, year = entity.Date.Year },
            new DonneesXxxViewModel { Id = entity.Id, Date = entity.Date, Intensite = entity.Intensite, Commentaire = entity.Commentaire });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, DonneesXxxDto dto, CancellationToken ct)
    {
        var existing = await context.DonneesXxx.FindAsync([id], ct);
        if (existing is null) return NotFound();

        // Vérification sur l'entité en base, jamais sur dto.CarnetSanteId.
        var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, existing.CarnetSanteId);
        if (securityCheck != null) return securityCheck;

        existing.Date = dto.Date;
        existing.Intensite = dto.Intensite;
        existing.Commentaire = dto.Commentaire;

        await context.SaveChangesAsync(ct);
        carnetSanteService.InvalidateCache(existing.CarnetSanteId);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var existing = await context.DonneesXxx.FindAsync([id], ct);
        if (existing is null) return NotFound();

        var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, existing.CarnetSanteId);
        if (securityCheck != null) return securityCheck;

        context.DonneesXxx.Remove(existing);
        await context.SaveChangesAsync(ct);
        carnetSanteService.InvalidateCache(existing.CarnetSanteId);
        return NoContent();
    }
}
```

## Côté client
```ts
// features/<domaine>/types/donnees-xxx.ts
export interface DonneesXxx {
  id: number
  date: string
  intensite: number
  commentaire?: string
}

// shared/services/apiService.ts (dans la classe ApiService)
async getDonneesXxxByMonth(carnetSanteId: number, month: number, year: number): Promise<DonneesXxx[]> {
    const data = await this.request<{ $values: DonneesXxx[] }>('GET', `DonneesXxx/by-month/${carnetSanteId}/${month}/${year}`);
    return data.$values;
}
```
