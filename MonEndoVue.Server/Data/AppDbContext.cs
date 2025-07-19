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
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuration des relations pour éviter les cascades multiples
        modelBuilder.Entity<DonneesDouleur>()
            .HasOne<CarnetSante>()
            .WithMany(c => c.DonneesDouleurs)
            .HasForeignKey(d => d.CarnetSanteId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<DonneesActivitePhysique>()
            .HasOne<CarnetSante>()
            .WithMany(c => c.DonneesActivitePhysique)
            .HasForeignKey(d => d.CarnetSanteId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<DonneesTransit>()
            .HasOne<CarnetSante>()
            .WithMany(c => c.DonneesTransit)
            .HasForeignKey(d => d.CarnetSanteId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<JourRegle>()
            .HasOne<CarnetSante>()
            .WithMany(c => c.JourRegles)
            .HasForeignKey(j => j.CarnetSanteId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<BilanQuotidien>()
            .HasOne<CarnetSante>()
            .WithMany(c => c.BilansQuotidiens)
            .HasForeignKey(b => b.CarnetSanteId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<SymptomeCycle>()
            .HasOne<CarnetSante>()
            .WithMany(c => c.SymptomesCycles)
            .HasForeignKey(s => s.CarnetSanteId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // CarnetSante → ApplicationUser (CASCADE OK)
        modelBuilder.Entity<CarnetSante>()
            .HasOne(c => c.User)
            .WithOne(u => u.CarnetSante)
            .HasForeignKey<CarnetSante>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Medicament → CarnetSante (CASCADE OK)
        modelBuilder.Entity<Medicament>()
            .HasOne(m => m.CarnetSante)
            .WithMany(c => c.Medicaments)
            .HasForeignKey(m => m.CarnetSanteId)
            .OnDelete(DeleteBehavior.Cascade);

        // DonneesMedicament → CarnetSante (CASCADE OK)
        modelBuilder.Entity<DonneesMedicament>()
            .HasOne<CarnetSante>()
            .WithMany(dm => dm.DonneesMedicaments)
            .HasForeignKey(d => d.CarnetSanteId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // DonneesMedicament → Medicament (NO ACTION pour éviter cascade multiple)
        modelBuilder.Entity<DonneesMedicament>()
            .HasOne(d => d.Medicament)
            .WithMany()
            .HasForeignKey(d => d.MedicamentId)
            .OnDelete(DeleteBehavior.Restrict); // ← IMPORTANT : Pas de cascade
    }
}