using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Services;

public class CarnetSanteService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CarnetSanteService> _logger;
    private readonly IMemoryCache _cache;


    public CarnetSanteService(AppDbContext context, ILogger<CarnetSanteService> logger, IMemoryCache cache)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
    }

    public async Task<CarnetSante> GetCarnetSanteId(string userId)
    {
        var carnetSante = await _context.CarnetSantes
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (carnetSante == null)
        {
            throw new Exception("Carnet de santé introuvable");
        }

        return carnetSante;
    }

    public async Task<CarnetViewModel> GetCarnetSanteByUsername(string username)
    {
        var carnetSante = await _context.CarnetSantes
            .Include(c => c.User)
            .Include(c => c.DonneesDouleurs)
            .Include(c => c.DonneesActivitePhysique)
            .Include(c => c.Medicaments)
            .Include(c => c.DonneesMedicaments)
            .FirstOrDefaultAsync(c => c.User.UserName == username);

        if (carnetSante == null)
        {
            throw new Exception("Carnet de santé introuvable");
        }

        return new CarnetViewModel
        {
            UserName = carnetSante.User?.UserName,
            CarnetSanteId = carnetSante.Id,
            DonneesDouleur = carnetSante.DonneesDouleurs,
            DonneesActivitePhysique = carnetSante.DonneesActivitePhysique
        };
    }

    public async Task<CarnetViewModel> GetCarnetSanteById(int carnetSanteId)
    {
        _logger.LogInformation("GetCarnetSanteById called with id: {Id}", carnetSanteId);
        
        var carnetSante = await _context.CarnetSantes
            .Include(c => c.User)
            .Include(c => c.DonneesDouleurs.OrderBy(d => d.Date))
            .Include(c => c.DonneesActivitePhysique.OrderBy(d => d.Date))
            .Include(c => c.Medicaments)
            .Include(c => c.DonneesMedicaments.OrderBy(d => d.Date))
            .Include(c => c.DonneesTransit.OrderBy(d => d.Date))
            .Include(c => c.JourRegles.OrderBy(d => d.Date))
            .FirstOrDefaultAsync(c => c.Id == carnetSanteId);

        if (carnetSante == null)
        {
            throw new Exception("Carnet de santé introuvable");
        }

        var donneesMedicamentViewModel = carnetSante.DonneesMedicaments.Select(dm => new DonneesMedicamentViewModel
        {
            Id = dm.Id,
            NomMedicament = carnetSante.Medicaments.FirstOrDefault(m => m.Id == dm.MedicamentId)?.Nom!,
            NombreComprimes = dm.NombreComprimes,
            Date = dm.Date,
            Commentaire = dm.Commentaire
        });

        return new CarnetViewModel
        {
            UserName = carnetSante.User?.UserName,
            CarnetSanteId = carnetSante.Id,
            DonneesDouleur = carnetSante.DonneesDouleurs,
            DonneesActivitePhysique = carnetSante.DonneesActivitePhysique,
            DonneesTransit = carnetSante.DonneesTransit,
            Medicaments = carnetSante.Medicaments,
            DonneesMedicament = donneesMedicamentViewModel,
            JourRegles = carnetSante.JourRegles
        };
    }


    public async Task<CarnetHomepageViewModel> GetLastEntries(int carnetSanteId)
    {
        _logger.LogInformation("GetLastEntries called with id: {Id}", carnetSanteId);

        var cacheKey = $"CarnetSante_LastEntries_{carnetSanteId}";
        if (_cache.TryGetValue(cacheKey, out CarnetHomepageViewModel cachedData)) return cachedData;
        var carnetSante = await _context.CarnetSantes
            .AsSplitQuery() 
            .Include(c => c.User)
            .Include(c => c.DonneesDouleurs.OrderByDescending(d => d.Date).Take(1))
            .Include(c => c.DonneesActivitePhysique.OrderByDescending(d => d.Date).Take(1))
            .Include(c => c.DonneesMedicaments.OrderByDescending(d => d.Date).Take(1))
            .ThenInclude(donneesMedicament => donneesMedicament.Medicament)
            .Include(c => c.DonneesTransit.OrderByDescending(d => d.Date).Take(1))
            .Include(c => c.JourRegles.OrderByDescending(d => d.Date).Take(1))
            .FirstOrDefaultAsync(c => c.Id == carnetSanteId);;

        if (carnetSante == null)
        {
            throw new Exception("Carnet de santé introuvable");
        }

        var derniereDonneesDouleur = carnetSante.DonneesDouleurs.SingleOrDefault();
        var derniereDonneesActivitePhysique = carnetSante.DonneesActivitePhysique.SingleOrDefault();
        var derniereDonneesMedicaments = carnetSante.DonneesMedicaments.SingleOrDefault();
        var derniereDonneesTransit = carnetSante.DonneesTransit.SingleOrDefault();
        var dernierJourRegle = carnetSante.JourRegles.SingleOrDefault();

        cachedData = new CarnetHomepageViewModel
        {
            UserName = carnetSante.User?.UserName,
            CarnetSanteId = carnetSante.Id,
            DonneesDouleur = derniereDonneesDouleur,
            DonneesActivitePhysique = derniereDonneesActivitePhysique,
            DonneesMedicament = derniereDonneesMedicaments,
            DonneesTransit = derniereDonneesTransit,
            NomMedicament = derniereDonneesMedicaments?.Medicament?.Nom,
            JourRegle = dernierJourRegle
        };

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(120));

        _cache.Set(cacheKey, cachedData, cacheEntryOptions);

        return cachedData;
    }

    public async Task<CarnetViewModel> GetDonneesCarnetSanteByMonth(int carnetSanteId, int month, int year)
    {
        _logger.LogInformation("GetDonneesCarnetSanteByMonth called with id: {Id}, month: {Month}, year: {Year}",
            carnetSanteId, month, year);
        
        var carnetSante = await _context.CarnetSantes
            .Include(c => c.User)
            .Include(c => c.DonneesDouleurs.Where(d => d.Date.Month == month && d.Date.Year == year).OrderBy(d => d.Date))
            .Include(c => c.DonneesActivitePhysique.Where(d => d.Date.Month == month && d.Date.Year == year).OrderBy(d => d.Date))
            .Include(c => c.Medicaments)
            .Include(c => c.DonneesMedicaments.Where(d => d.Date.Month == month && d.Date.Year == year).OrderBy(d => d.Date))
            .Include(c => c.DonneesTransit.Where(d => d.Date.Month == month && d.Date.Year == year).OrderBy(d => d.Date))
            .Include(c => c.JourRegles.Where(d => d.Date.Month == month && d.Date.Year == year).OrderBy(d => d.Date))
            .Include(c => c.BilansQuotidiens.Where(d => d.Date.Month == month && d.Date.Year == year).OrderBy(d => d.Date))
            .FirstOrDefaultAsync(c => c.Id == carnetSanteId);
        
        if (carnetSante == null)
        {
            throw new Exception("Carnet de santé introuvable");
        }

        var donneesMedicamentViewModel = carnetSante.DonneesMedicaments.Select(dm => new DonneesMedicamentViewModel
        {
            Id = dm.Id,
            NomMedicament = carnetSante.Medicaments.FirstOrDefault(m => m.Id == dm.MedicamentId)?.Nom!,
            Date = dm.Date,
            Commentaire = dm.Commentaire
        });

        return new CarnetViewModel
        {
            UserName = carnetSante.User?.UserName,
            CarnetSanteId = carnetSante.Id,
            DonneesDouleur = carnetSante.DonneesDouleurs,
            DonneesActivitePhysique = carnetSante.DonneesActivitePhysique,
            DonneesTransit = carnetSante.DonneesTransit,
            Medicaments = carnetSante.Medicaments,
            DonneesMedicament = donneesMedicamentViewModel,
            JourRegles = carnetSante.JourRegles,
            BilansQuotidiens = carnetSante.BilansQuotidiens
        };
    }
    
    public void InvalidateCache(int carnetSanteId)
    {
        var cacheKey = $"CarnetSante_LastEntries_{carnetSanteId}";
        _cache.Remove(cacheKey);
    }

    public async Task CreateCarnetSante(string userId)
    {
        var carnetSante = new CarnetSante
        {
            UserId = userId
        };

        await _context.CarnetSantes.AddAsync(carnetSante);
        await _context.SaveChangesAsync();
    }
}