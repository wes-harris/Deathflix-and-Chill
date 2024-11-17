using DeathflixAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DeathflixAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Define your DbSet properties here
        public DbSet<Product> Products { get; set; }
    }
}