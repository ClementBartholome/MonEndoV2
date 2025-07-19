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
        var carnetSante = await _context.CarnetSantes
            .Where(c => c.Id == carnetSanteId)
            .Select(c => new CarnetHomepageViewModel
            {
                UserName = c.User.UserName,
                CarnetSanteId = c.Id,
                DonneesDouleur = c.DonneesDouleurs
                    .OrderByDescending(d => d.Date)
                    .Select(d => new DonneesDouleurViewModel
                    {
                        TypeDouleur = d.TypeDouleur,
                        Date = d.Date
                    })
                    .FirstOrDefault(),
                DonneesActivitePhysique = c.DonneesActivitePhysique
                    .OrderByDescending(a => a.Date)
                    .Select(a => new DonneesActivitePhysiqueViewModel
                    {
                        TypeActivite = a.TypeActivite,
                        Date = a.Date
                    })
                    .FirstOrDefault(),
                DonneesMedicament = c.DonneesMedicaments
                    .OrderByDescending(m => m.Date)
                    .Select(m => new DonneesMedicamentHomepageViewModel
                    {
                        Id = m.Id,
                        NomMedicament = m.Medicament.Nom,
                        Date = m.Date,
                    })
                    .FirstOrDefault(),
                DonneesTransit = c.DonneesTransit
                    .OrderByDescending(t => t.Date)
                    .Select(t => new DonneesTransitViewModel
                    {
                        TypeEvenement = t.TypeEvenement,
                        Date = t.Date
                    })
                    .FirstOrDefault(),
                JourRegle = c.JourRegles
                    .OrderByDescending(j => j.Date)
                    .Select(j => new JourRegleViewModel
                    {
                        Date = j.Date
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (carnetSante == null)
            throw new Exception("Carnet de santé introuvable");

        return carnetSante;
    }

    public async Task<CarnetViewModel> GetDonneesCarnetSanteByMonth(int carnetSanteId, int month, int year)
    {
        _logger.LogInformation("GetDonneesCarnetSanteByMonth called with id: {Id}, month: {Month}, year: {Year}",
            carnetSanteId, month, year);

        var carnetSante = await _context.CarnetSantes
            .Include(c => c.User)
            .Include(c =>
                c.DonneesDouleurs.Where(d => d.Date.Month == month && d.Date.Year == year).OrderBy(d => d.Date))
            .Include(c =>
                c.DonneesActivitePhysique.Where(d => d.Date.Month == month && d.Date.Year == year).OrderBy(d => d.Date))
            .Include(c => c.Medicaments)
            .Include(c =>
                c.DonneesMedicaments.Where(d => d.Date.Month == month && d.Date.Year == year).OrderBy(d => d.Date))
            .Include(c =>
                c.DonneesTransit.Where(d => d.Date.Month == month && d.Date.Year == year).OrderBy(d => d.Date))
            .Include(c => c.JourRegles.Where(d => d.Date.Month == month && d.Date.Year == year).OrderBy(d => d.Date))
            .Include(c =>
                c.BilansQuotidiens.Where(d => d.Date.Month == month && d.Date.Year == year).OrderBy(d => d.Date))
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