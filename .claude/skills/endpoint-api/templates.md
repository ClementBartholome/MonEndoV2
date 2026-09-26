# Gabarits API MonEndo (SOLID)

Remplacer `Xxx` par le nom de l'entité (`DonneesHydratation`…). Namespaces file-scoped, primary constructors.
Découpage : **DTO / ViewModel** (contrats) → **service** (métier, requêtes EF, contrôle de propriété) → **contrôleur** (HTTP uniquement).

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

## Résultat d'opération — `Services/ResultatOperation.cs` (à créer au premier usage, puis réutiliser)
Le service ne connaît pas HTTP : il renvoie un statut métier, que le contrôleur traduit en réponse.
```csharp
namespace MonEndoVue.Server.Services;

public enum StatutOperation { Succes, Introuvable, Interdit }

public sealed record ResultatOperation<T>(StatutOperation Statut, T? Valeur)
{
    public static ResultatOperation<T> Succes(T valeur) => new(StatutOperation.Succes, valeur);
    public static ResultatOperation<T> Introuvable() => new(StatutOperation.Introuvable, default);
    public static ResultatOperation<T> Interdit() => new(StatutOperation.Interdit, default);
}
```

## Service — `Services/DonneesXxxService.cs` (`AddScoped<DonneesXxxService>()` dans `Program.cs`)
```csharp
using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Services;

/// <summary>Données Xxx d'un carnet : lecture, création, modification, suppression, propriété vérifiée ici.</summary>
public class DonneesXxxService(AppDbContext context, CarnetSanteService carnetSanteService)
{
    private static DonneesXxxViewModel VersViewModel(DonneesXxx x) =>
        new() { Id = x.Id, Date = x.Date, Intensite = x.Intensite, Commentaire = x.Commentaire };

    private Task<bool> EstProprietaireAsync(string userId, int carnetSanteId, CancellationToken ct) =>
        context.CarnetSantes.AnyAsync(c => c.Id == carnetSanteId && c.UserId == userId, ct);

    public async Task<ResultatOperation<List<DonneesXxxViewModel>>> ListerParMoisAsync(
        string userId, int carnetSanteId, int mois, int annee, CancellationToken ct)
    {
        if (!await EstProprietaireAsync(userId, carnetSanteId, ct))
            return ResultatOperation<List<DonneesXxxViewModel>>.Interdit();

        var donnees = await context.DonneesXxx
            .Where(x => x.CarnetSanteId == carnetSanteId && x.Date.Month == mois && x.Date.Year == annee)
            .OrderByDescending(x => x.Date)
            .ToListAsync(ct);
        return ResultatOperation<List<DonneesXxxViewModel>>.Succes(donnees.Select(VersViewModel).ToList());
    }

    public async Task<ResultatOperation<DonneesXxxViewModel>> CreerAsync(string userId, DonneesXxxDto dto, CancellationToken ct)
    {
        if (!await EstProprietaireAsync(userId, dto.CarnetSanteId, ct))
            return ResultatOperation<DonneesXxxViewModel>.Interdit();

        var entite = new DonneesXxx
        {
            CarnetSanteId = dto.CarnetSanteId,
            Date = dto.Date,
            Intensite = dto.Intensite,
            Commentaire = dto.Commentaire,
        };
        context.DonneesXxx.Add(entite);
        await context.SaveChangesAsync(ct);
        carnetSanteService.InvalidateCache(entite.CarnetSanteId);
        return ResultatOperation<DonneesXxxViewModel>.Succes(VersViewModel(entite));
    }

    public async Task<StatutOperation> ModifierAsync(string userId, int id, DonneesXxxDto dto, CancellationToken ct)
    {
        var existante = await context.DonneesXxx.FindAsync([id], ct);
        if (existante is null) return StatutOperation.Introuvable;
        // Propriété vérifiée sur l'entité en base, jamais sur dto.CarnetSanteId.
        if (!await EstProprietaireAsync(userId, existante.CarnetSanteId, ct)) return StatutOperation.Interdit;

        existante.Date = dto.Date;
        existante.Intensite = dto.Intensite;
        existante.Commentaire = dto.Commentaire;
        await context.SaveChangesAsync(ct);
        carnetSanteService.InvalidateCache(existante.CarnetSanteId);
        return StatutOperation.Succes;
    }

    public async Task<StatutOperation> SupprimerAsync(string userId, int id, CancellationToken ct)
    {
        var existante = await context.DonneesXxx.FindAsync([id], ct);
        if (existante is null) return StatutOperation.Introuvable;
        if (!await EstProprietaireAsync(userId, existante.CarnetSanteId, ct)) return StatutOperation.Interdit;

        context.DonneesXxx.Remove(existante);
        await context.SaveChangesAsync(ct);
        carnetSanteService.InvalidateCache(existante.CarnetSanteId);
        return StatutOperation.Succes;
    }
}
```

## Contrôleur — `Controllers/DonneesXxxController.cs` (HTTP uniquement)
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Dto;
using MonEndoVue.Server.Services;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize]
public class DonneesXxxController(DonneesXxxService service) : ControllerBase
{
    private IActionResult Reponse(StatutOperation statut, Func<IActionResult> succes) => statut switch
    {
        StatutOperation.Succes => succes(),
        StatutOperation.Interdit => Forbid(),
        _ => NotFound(),
    };

    [HttpGet("by-month/{carnetSanteId:int}/{month:int}/{year:int}")]
    public async Task<IActionResult> GetByMonth(int carnetSanteId, int month, int year, CancellationToken ct)
    {
        var resultat = await service.ListerParMoisAsync(User.GetCurrentUserId(), carnetSanteId, month, year, ct);
        return Reponse(resultat.Statut, () => Ok(resultat.Valeur));
    }

    [HttpPost]
    public async Task<IActionResult> Post(DonneesXxxDto dto, CancellationToken ct)
    {
        var resultat = await service.CreerAsync(User.GetCurrentUserId(), dto, ct);
        return Reponse(resultat.Statut, () => StatusCode(StatusCodes.Status201Created, resultat.Valeur));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, DonneesXxxDto dto, CancellationToken ct) =>
        Reponse(await service.ModifierAsync(User.GetCurrentUserId(), id, dto, ct), NoContent);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        Reponse(await service.SupprimerAsync(User.GetCurrentUserId(), id, ct), NoContent);
}
```
Tests : le service se teste avec `Support/CarnetDeTest.cs` (propriété, introuvable, copie des champs) ; le contrôleur,
devenu trivial, n'a besoin que d'un test par traduction de statut.

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
