using Microsoft.EntityFrameworkCore;
using SistemasPrecios.Api.Domain;

namespace SistemasPrecios.Api.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();
    public DbSet<ExtractedLine> ExtractedLines => Set<ExtractedLine>();
    public DbSet<CanonicalProduct> CanonicalProducts => Set<CanonicalProduct>();
    public DbSet<ProductAlias> ProductAliases => Set<ProductAlias>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
    public DbSet<ProductMatch> ProductMatches => Set<ProductMatch>();
    public DbSet<ComparisonResult> ComparisonResults => Set<ComparisonResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(user => user.Email)
            .IsUnique();

        modelBuilder.Entity<Supplier>()
            .HasIndex(supplier => supplier.Name)
            .IsUnique();

        modelBuilder.Entity<CanonicalProduct>()
            .HasIndex(product => product.Name)
            .IsUnique();

        modelBuilder.Entity<ProductAlias>()
            .HasIndex(alias => alias.Alias);

        modelBuilder.Entity<Document>()
            .HasMany(document => document.ExtractedLines)
            .WithOne(line => line.Document)
            .HasForeignKey(line => line.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExtractedLine>()
            .HasMany(line => line.Matches)
            .WithOne(match => match.ExtractedLine)
            .HasForeignKey(match => match.ExtractedLineId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CanonicalProduct>()
            .HasMany(product => product.Aliases)
            .WithOne(alias => alias.CanonicalProduct)
            .HasForeignKey(alias => alias.CanonicalProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PriceSnapshot>()
            .HasIndex(snapshot => new
            {
                snapshot.CanonicalProductId,
                snapshot.SupplierId,
                snapshot.EffectiveAt
            });

        modelBuilder.Entity<ComparisonResult>()
            .HasIndex(result => result.CanonicalProductId)
            .IsUnique();
    }
}
