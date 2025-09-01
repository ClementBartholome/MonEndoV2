using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MonEndoVue.Server.Data;
using MonEndoVue.Server.Models;
using MonEndoVue.Server.ViewModels;

namespace MonEndoVue.Server.Services;

public class CarnetSanteService(AppDbContext context, ILogger<CarnetSanteService> logger, IMemoryCache cache)
{
    private async Task<bool> IsCarnetOwner(int carnetSanteId, string userId)
    {
        return await context.CarnetSantes
            .AnyAsync(c => c.Id == carnetSanteId && c.UserId == userId);
    }
    
    public async Task<int> GetCarnetSanteId(string userId)
    {
        var carnetSante = await context.CarnetSantes
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (carnetSante == null)
        {
            throw new Exception("Carnet de santé introuvable");
        }

        return carnetSante.Id;
    }

    public async Task<CarnetSante> GetCarnetSanteByUserId(string userId)
    {
        var carnetSante = await context.CarnetSantes
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (carnetSante == null)
        {
            throw new Exception("Carnet de santé introuvable");
        }

        return carnetSante;
    }

    public async Task<CarnetViewModel> GetCarnetSanteByUsername(string username)
    {
        var carnetSante = await context.CarnetSantes
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

    public async Task<CarnetViewModel> GetCarnetSanteById(int carnetSanteId, string userId)
    {
        logger.LogInformation("GetCarnetSanteById called with id: {Id} for user: {UserId}", carnetSanteId, userId);

        // Vérification de sécurité : s'assurer que le carnet appartient à l'utilisateur
        if (!await IsCarnetOwner(carnetSanteId, userId))
        {
            throw new UnauthorizedAccessException("Accès non autorisé à ce carnet de santé");
        }

        var carnetSante = await context.CarnetSantes
            .Include(c => c.User)
            .Include(c => c.DonneesDouleurs.OrderBy(d => d.Date))
            .Include(c => c.DonneesActivitePhysique.OrderBy(d => d.Date))
            .Include(c => c.Medicaments)
            .Include(c => c.DonneesMedicaments.OrderBy(d => d.Date))
            .Include(c => c.DonneesTransit.OrderBy(d => d.Date))
            .Include(c => c.JourRegles.OrderBy(d => d.Date))
            .Where(c => c.Id == carnetSanteId && c.UserId == userId) // Double vérification dans la requête
            .FirstOrDefaultAsync();

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

    public async Task<CarnetHomepageViewModel> GetLastEntries(int carnetSanteId, string userId)
    {
        // Vérification de sécurité
        if (!await IsCarnetOwner(carnetSanteId, userId))
        {
            throw new UnauthorizedAccessException("Accès non autorisé à ce carnet de santé");
        }

        var carnetSante = await context.CarnetSantes
            .Where(c => c.Id == carnetSanteId && c.UserId == userId) // Filtrer par userId
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

    public async Task<CarnetHomepageViewModel> GetLastEntriesByUserId(string userId)
    {
        var carnetSante = await context.CarnetSantes
            .Where(c => c.UserId == userId)
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

    public async Task<CarnetPdfExportViewModel> GetDonneesCarnetSanteByMonthForPdf(int carnetSanteId, int month, int year, string userId)
    {
        if (!await IsCarnetOwner(carnetSanteId, userId))
        {
            throw new UnauthorizedAccessException("Accès non autorisé à ce carnet de santé");
        }

        var carnetSante = await context.CarnetSantes
            .Include(c => c.User)
            .Include(c => c.Medicaments)
            .Include(c => c.DonneesMedicaments.Where(d => d.Date.Month == month && d.Date.Year == year))
            .Include(c => c.DonneesDouleurs.Where(d => d.Date.Month == month && d.Date.Year == year))
            .Include(c => c.DonneesActivitePhysique.Where(d => d.Date.Month == month && d.Date.Year == year))
            .Include(c => c.DonneesTransit.Where(d => d.Date.Month == month && d.Date.Year == year))
            .Include(c => c.JourRegles.Where(d => d.Date.Month == month && d.Date.Year == year))
            .Include(c => c.BilansQuotidiens.Where(d => d.Date.Month == month && d.Date.Year == year))
            .FirstOrDefaultAsync(c => c.Id == carnetSanteId && c.UserId == userId); // Filtrer par userId

        if (carnetSante == null)
            throw new Exception("Carnet de santé introuvable");

        return new CarnetPdfExportViewModel
        {
            UserName = carnetSante.User?.UserName,
            CarnetSanteId = carnetSante.Id,
            Medicaments = carnetSante.Medicaments.Select(m => new CarnetPdfMedicamentViewModel
            {
                Id = m.Id,
                Nom = m.Nom
            }).ToList(),
            DonneesMedicament = carnetSante.DonneesMedicaments.Select(dm => new DonneesMedicamentExportViewModel
            {
                Id = dm.Id,
                MedicamentId = dm.MedicamentId,
                NomMedicament = carnetSante.Medicaments.FirstOrDefault(m => m.Id == dm.MedicamentId)?.Nom ?? "",
                Date = dm.Date
            }).ToList(),
            JourRegles = carnetSante.JourRegles.Select(j => new JourRegleViewModel
            {
                Date = j.Date
            }).ToList(),
            DonneesDouleur = carnetSante.DonneesDouleurs.Select(d => new DonneesDouleurExportViewModel
            {
                TypeDouleur = d.TypeDouleur,
                Date = d.Date,
                Intensite = d.Intensite
            }).ToList(),
            DonneesActivitePhysique = carnetSante.DonneesActivitePhysique.Select(a =>
                new DonneesActivitePhysiqueExportViewModel
                {
                    TypeActivite = a.TypeActivite,
                    Date = a.Date,
                    Intensite = a.Intensite
                }).ToList(),
            DonneesTransit = carnetSante.DonneesTransit.Select(t => new DonneesTransitExportViewModel
            {
                TypeEvenement = t.TypeEvenement,
                Date = t.Date
            }).ToList(),
            BilansQuotidiens = carnetSante.BilansQuotidiens.Select(b => new BilanQuotidienExportViewModel
            {
                Date = b.Date,
                Lactose = b.Lactose,
                Grignotage = b.Grignotage,
                Gluten = b.Gluten,
                Fatigue = b.Fatigue,
                Pas = b.Pas,
                DouleurMoyenne = b.DouleurMoyenne,
                Hydratation = b.Hydratation,
                StressMoyenne = b.StressPro + b.StressPerso > 0
                    ? (b.StressPro + b.StressPerso) / 2.0
                    : 0
            }).ToList()
        };
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