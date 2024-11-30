using Microsoft.EntityFrameworkCore;
using DeathflixAPI.Models;
using DeathflixAPI.Converters;

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
    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<DateOnly>()
            .HaveConversion<DateOnlyConverter>()
            .HaveColumnType("date");
    }
}