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
    public DbSet<DonneesTraitementNonMedicamenteux> DonneesTraitementNonMedicamenteux { get; set; }
    public DbSet<DonneesTransit> DonneesTransit { get; set; }
    public DbSet<JourRegle> JourRegles { get; set; }
    public DbSet<BilanQuotidien> BilansQuotidiens { get; set; }
    public DbSet<SymptomeCycle> SymptomesCycles { get; set; }
    public DbSet<AbonnementPush> AbonnementsPush { get; set; }
    public DbSet<Rappel> Rappels { get; set; }
    public DbSet<LiaisonAgenda> LiaisonsAgenda { get; set; }
    public DbSet<EpisodeAcne> EpisodesAcne { get; set; }
    
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

        // Les émotions n'existent qu'au sein de leur bilan : type possédé, chargé et supprimé avec lui.
        modelBuilder.Entity<Medicament>().OwnsMany(m => m.Horaires, horaire =>
        {
            horaire.ToTable("HorairesPrise");
            horaire.WithOwner().HasForeignKey("MedicamentId");
            horaire.HasKey(h => h.Id);
            horaire.HasIndex("MedicamentId", nameof(HorairePrise.Heure)).IsUnique();
        });
        modelBuilder.Entity<Medicament>().Property(m => m.Frequence).HasConversion<string>().HasMaxLength(20)
            .HasDefaultValue(FrequencePrise.AuBesoin).HasSentinel((FrequencePrise)(-1));
        modelBuilder.Entity<DonneesMedicament>().Property(p => p.Statut).HasConversion<string>().HasMaxLength(10)
            .HasDefaultValue(StatutPrise.Pris).HasSentinel((StatutPrise)(-1));

        modelBuilder.Entity<BilanQuotidien>().OwnsMany(b => b.Emotions, emotion =>
        {
            emotion.ToTable("EmotionsBilan");
            emotion.WithOwner().HasForeignKey("BilanQuotidienId");
            emotion.HasKey(e => e.Id);
            emotion.Property(e => e.Emotion).HasConversion<string>().HasMaxLength(32);
            emotion.HasIndex("BilanQuotidienId", nameof(EmotionBilan.Emotion)).IsUnique();
        });
        
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

        // DonneesTraitementNonMedicamenteux → CarnetSante (CASCADE OK)
        modelBuilder.Entity<DonneesTraitementNonMedicamenteux>()
            .HasOne<CarnetSante>()
            .WithMany(c => c.DonneesTraitementNonMedicamenteux)
            .HasForeignKey(d => d.CarnetSanteId)
            .OnDelete(DeleteBehavior.Cascade);

        // DonneesTraitementNonMedicamenteux → Medicament (NO ACTION pour éviter cascade multiple)
        modelBuilder.Entity<DonneesTraitementNonMedicamenteux>()
            .HasOne(d => d.Medicament)
            .WithMany()
            .HasForeignKey(d => d.MedicamentId)
            .OnDelete(DeleteBehavior.Restrict); // ← IMPORTANT : Pas de cascade

        // Notifications Web Push : un abonnement par appareil (endpoint unique), supprimés avec le carnet
        modelBuilder.Entity<AbonnementPush>(entity =>
        {
            entity.Property(a => a.Endpoint).HasMaxLength(450);
            entity.HasIndex(a => a.Endpoint).IsUnique();
            entity.HasOne(a => a.CarnetSante)
                .WithMany()
                .HasForeignKey(a => a.CarnetSanteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Rappel>(entity =>
        {
            entity.Property(r => r.Type).HasConversion<string>().HasMaxLength(32);
            entity.Property(r => r.FuseauHoraire).HasMaxLength(64);
            entity.HasIndex(r => new { r.CarnetSanteId, r.Type }).IsUnique();
            entity.HasOne(r => r.CarnetSante)
                .WithMany()
                .HasForeignKey(r => r.CarnetSanteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Liaison à l'agenda Google : une par carnet, supprimée avec lui
        modelBuilder.Entity<LiaisonAgenda>(entity =>
        {
            entity.ToTable("LiaisonsAgenda");
            entity.HasIndex(l => l.CarnetSanteId).IsUnique();
            entity.HasOne(l => l.CarnetSante)
                .WithMany()
                .HasForeignKey(l => l.CarnetSanteId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(l => l.CalendrierId).HasMaxLength(1024);
        });

        modelBuilder.Entity<EpisodeAcne>(entity =>
        {
            entity.ToTable("EpisodesAcne");
            entity.HasIndex(e => new { e.CarnetSanteId, e.Debut });
            entity.HasOne<CarnetSante>()
                .WithMany()
                .HasForeignKey(e => e.CarnetSanteId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}