using System.ComponentModel.DataAnnotations;

namespace DeathflixAPI.Models;

public class MovieCredit
{
    // Primary key for the credit
    public int Id { get; set; }

    // These are foreign keys - they reference the Actor and Movie tables
    [Required]
    public int ActorId { get; set; }

    [Required]
    public int MovieId { get; set; }

    // The character name the actor played in the movie
    [Required]
    [StringLength(100)]
    public string Character { get; set; }

    // Department (like "Acting", "Directing", etc.)
    public string? Department { get; set; }

    // Specific role (like "Lead Actor", "Supporting Actor", etc.)
    [StringLength(100)]
    public string? Role { get; set; }

    // Navigation properties - these create the connections between tables
    // When you load a MovieCredit, you can access the related Actor and Movie
    public virtual Actor Actor { get; set; }
    public virtual Movie Movie { get; set; }
}