using Microsoft.EntityFrameworkCore;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<DonneesDouleur> DonneesDouleurs { get; set; }
    public DbSet<DonneesActivitePhysique> DonneesActivitePhysique { get; set; }
    public DbSet<CarnetSante> CarnetSantes { get; set; }
    public DbSet<Medicament> Medicaments { get; set; }
    public DbSet<DonneesMedicament> DonneesMedicaments { get; set; }
    public DbSet<DonneesTransit> DonneesTransit { get; set; }
    public DbSet<JourRegle> JourRegles { get; set; }
    public DbSet<BilanQuotidien> BilansQuotidiens { get; set; }
    public DbSet<DeviceToken> DeviceTokens { get; set; }
    public DbSet<SymptomeCycle> SymptomesCycles { get; set; }
}