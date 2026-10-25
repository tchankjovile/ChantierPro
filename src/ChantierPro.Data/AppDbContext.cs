using ChantierPro.Core.Models;
using ChantierPro.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChantierPro.Data;

/// <summary>
/// Contexte EF Core de ChantierPro. Les entités métier de ChantierPro.Core sont persistées telles quelles
/// (aucun doublon de modèle) ; les propriétés calculées (Cost, TotalCost, IsReadOnly) sont ignorées.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Chantier> Chantiers => Set<Chantier>();
    public DbSet<Phase> Phases => Set<Phase>();
    public DbSet<PhaseTask> PhaseTasks => Set<PhaseTask>();
    public DbSet<ProgressEntry> ProgressEntries => Set<ProgressEntry>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Worker> Workers => Set<Worker>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<AlertItem> Alerts => Set<AlertItem>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<SitePhoto> Photos => Set<SitePhoto>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Montants en FCFA, quantités, pourcentages : 2 décimales suffisent (RG-08 arrondit déjà à 2).
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserAccount>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(150);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.PasswordHash).HasMaxLength(512);
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).HasMaxLength(128);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Chantier>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Reference).HasMaxLength(40);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Client).HasMaxLength(200);
            e.Property(x => x.Address).HasMaxLength(300);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            e.HasIndex(x => x.Reference).IsUnique();
            e.Ignore(x => x.IsReadOnly);
        });

        modelBuilder.Entity<Phase>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasIndex(x => new { x.ChantierId, x.Order });
            e.HasOne<Chantier>().WithMany().HasForeignKey(x => x.ChantierId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Tasks).WithOne().HasForeignKey(t => t.PhaseId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.History).WithOne().HasForeignKey(h => h.PhaseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PhaseTask>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Label).HasMaxLength(300);
        });

        modelBuilder.Entity<ProgressEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Author).HasMaxLength(150);
            e.Property(x => x.Note).HasMaxLength(2000);
        });

        modelBuilder.Entity<Material>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Category).HasMaxLength(100);
            e.Property(x => x.Unit).HasMaxLength(50);
            e.Property(x => x.Barcode).HasMaxLength(64);
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.Barcode);
        });

        modelBuilder.Entity<Supplier>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Address).HasMaxLength(300);
        });

        // Les clés étrangères hors chantier sont en « Restrict » : évite les chemins de cascade multiples (SQL Server).
        modelBuilder.Entity<StockMovement>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.DeliveryNoteNumber).HasMaxLength(100);
            e.Property(x => x.Author).HasMaxLength(150);
            e.HasIndex(x => new { x.ChantierId, x.MaterialId });
            e.HasOne<Chantier>().WithMany().HasForeignKey(x => x.ChantierId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Material>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Phase>().WithMany().HasForeignKey(x => x.PhaseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            e.Ignore(x => x.TotalCost);
        });

        modelBuilder.Entity<Worker>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(150);
            e.Property(x => x.Trade).HasMaxLength(150);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<Attendance>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            e.HasIndex(x => new { x.ChantierId, x.WorkerId, x.Date }).IsUnique();
            e.HasOne<Chantier>().WithMany().HasForeignKey(x => x.ChantierId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Worker>().WithMany().HasForeignKey(x => x.WorkerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Phase>().WithMany().HasForeignKey(x => x.PhaseId).OnDelete(DeleteBehavior.Restrict);
            e.Ignore(x => x.Cost);
        });

        modelBuilder.Entity<BudgetLine>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Category).HasConversion<string>().HasMaxLength(32);
            e.HasIndex(x => x.ChantierId);
            e.HasOne<Chantier>().WithMany().HasForeignKey(x => x.ChantierId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Expense>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Category).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.PaymentMode).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Supplier).HasMaxLength(200);
            e.Property(x => x.Label).HasMaxLength(300);
            e.Property(x => x.ReceiptPhotoPath).HasMaxLength(500);
            e.Property(x => x.Author).HasMaxLength(150);
            e.HasIndex(x => x.ChantierId);
            e.HasOne<Chantier>().WithMany().HasForeignKey(x => x.ChantierId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AlertItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Level).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Message).HasMaxLength(1000);
            e.Property(x => x.Key).HasMaxLength(200);
            e.HasIndex(x => new { x.ChantierId, x.Key });
            e.HasOne<Chantier>().WithMany().HasForeignKey(x => x.ChantierId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JournalEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Weather).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Note).HasMaxLength(4000);
            e.Property(x => x.Incident).HasMaxLength(2000);
            e.Property(x => x.Visitors).HasMaxLength(500);
            e.Property(x => x.Author).HasMaxLength(150);
            e.HasIndex(x => x.ChantierId);
            e.HasOne<Chantier>().WithMany().HasForeignKey(x => x.ChantierId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SitePhoto>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Path).HasMaxLength(500);
            e.Property(x => x.Stage).HasMaxLength(20);
            e.Property(x => x.Author).HasMaxLength(150);
            e.HasIndex(x => x.ChantierId);
            e.HasOne<Chantier>().WithMany().HasForeignKey(x => x.ChantierId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
