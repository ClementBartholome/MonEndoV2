using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services;

public class CarnetSanteService(AppDbContext context, IMemoryCache cache)
{
    public async Task<CarnetSante> GetCarnetSanteByUserId(string userId)
    {
        var carnetSante = await context.CarnetSantes
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (carnetSante == null)
        {
            throw new KeyNotFoundException("Carnet de santé introuvable");
        }

        return carnetSante;
    }

    public void InvalidateCache(int carnetSanteId)
    {
        var cacheKey = $"CarnetSante_LastEntries_{carnetSanteId}";
        cache.Remove(cacheKey);
    }

    public async Task CreateCarnetSante(string userId)
    {
        var carnetSante = new CarnetSante
        {
            UserId = userId
        };

        await context.CarnetSantes.AddAsync(carnetSante);
        await context.SaveChangesAsync();
    }
}