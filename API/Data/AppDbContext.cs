using Microsoft.EntityFrameworkCore;
using DeathflixAPI.Models;

namespace DeathflixAPI.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Actor> Actors => Set<Actor>();
    public DbSet<DeathRecord> DeathRecords => Set<DeathRecord>();
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<MovieCredit> MovieCredits => Set<MovieCredit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships
        modelBuilder.Entity<Actor>()
            .HasOne(a => a.DeathRecord)
            .WithOne(d => d.Actor)
            .HasForeignKey<DeathRecord>(d => d.ActorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MovieCredit>()
            .HasOne(mc => mc.Actor)
            .WithMany(a => a.MovieCredits)
            .HasForeignKey(mc => mc.ActorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MovieCredit>()
            .HasOne(mc => mc.Movie)
            .WithMany(m => m.Credits)
            .HasForeignKey(mc => mc.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure audit properties
        modelBuilder.Entity<Actor>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Actor>()
            .Property(e => e.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<DeathRecord>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<DeathRecord>()
            .Property(e => e.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}