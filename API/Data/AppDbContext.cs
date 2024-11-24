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

        // Indexes for Actor table
        modelBuilder.Entity<Actor>()
            .HasIndex(a => a.TmdbId)
            .IsUnique()
            .HasDatabaseName("IX_Actor_TmdbId");

        modelBuilder.Entity<Actor>()
            .HasIndex(a => a.Name)
            .HasDatabaseName("IX_Actor_Name");

        modelBuilder.Entity<Actor>()
            .HasIndex(a => a.DateOfDeath)
            .HasDatabaseName("IX_Actor_DateOfDeath");

        // Indexes for Movie table
        modelBuilder.Entity<Movie>()
            .HasIndex(m => m.TmdbId)
            .IsUnique()
            .HasDatabaseName("IX_Movie_TmdbId");

        modelBuilder.Entity<Movie>()
            .HasIndex(m => m.Title)
            .HasDatabaseName("IX_Movie_Title");

        modelBuilder.Entity<Movie>()
            .HasIndex(m => m.ReleaseDate)
            .HasDatabaseName("IX_Movie_ReleaseDate");

        // Indexes for MovieCredit table
        modelBuilder.Entity<MovieCredit>()
            .HasIndex(mc => new { mc.ActorId, mc.MovieId })
            .HasDatabaseName("IX_MovieCredit_ActorId_MovieId");

        // Index for DeathRecord table
        modelBuilder.Entity<DeathRecord>()
            .HasIndex(d => d.DateOfDeath)
            .HasDatabaseName("IX_DeathRecord_DateOfDeath");
    }
}