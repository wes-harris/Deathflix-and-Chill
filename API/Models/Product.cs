
namespace DeathflixAPI.Models
{
    public class Product
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public DateTime CreatedAt { get; internal set; }
    }
}