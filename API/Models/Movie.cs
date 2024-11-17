using System.ComponentModel.DataAnnotations;

namespace DeathflixAPI.Models;

public class Movie
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public int? TmdbId { get; set; }

    public DateTime? ReleaseDate { get; set; }

    // Navigation properties
    public virtual List<MovieCredit> Credits { get; set; } = new();
}