using System.ComponentModel.DataAnnotations;

namespace DeathflixAPI.Models;

public class Actor
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Biography { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public DateTime? DateOfDeath { get; set; }

    [MaxLength(200)]
    public string? PlaceOfBirth { get; set; }

    [MaxLength(200)]
    public string? PlaceOfDeath { get; set; }

    // External ID from TheMovieDB
    public int? TmdbId { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    // Navigation properties
    public virtual List<MovieCredit> MovieCredits { get; set; } = new();
    public virtual DeathRecord? DeathRecord { get; set; }

    // Audit properties
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}