using Microsoft.EntityFrameworkCore;
using DeathflixAPI.Models;

namespace DeathflixAPI.Data;

public class AppDbContext : DbContext
{
    // Constructor that takes DbContext options
    // This is where database connection settings will be passed in
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // DbSet properties - these represent our database tables
    public DbSet<Actor> Actors { get; set; }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<MovieCredit> MovieCredits { get; set; }
    public DbSet<DeathRecord> DeathRecords { get; set; }

    // This method lets us configure additional model relationships and database rules
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure one-to-one relationship between Actor and DeathRecord
        modelBuilder.Entity<Actor>()
            .HasOne(a => a.DeathRecord)
            .WithOne(d => d.Actor)
            .HasForeignKey<DeathRecord>(d => d.ActorId);

        // Configure one-to-many relationship between Actor and MovieCredit
        modelBuilder.Entity<Actor>()
            .HasMany(a => a.MovieCredits)
            .WithOne(mc => mc.Actor)
            .HasForeignKey(mc => mc.ActorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure one-to-many relationship between Movie and MovieCredit
        modelBuilder.Entity<Movie>()
            .HasMany(m => m.Credits)
            .WithOne(mc => mc.Movie)
            .HasForeignKey(mc => mc.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add indexes for better query performance
        modelBuilder.Entity<Actor>()
            .HasIndex(a => a.TmdbId)
            .IsUnique();

        modelBuilder.Entity<Movie>()
            .HasIndex(m => m.TmdbId)
            .IsUnique();

        modelBuilder.Entity<Actor>()
            .HasIndex(a => a.DateOfDeath);
    }
}