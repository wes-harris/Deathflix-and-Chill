using System.ComponentModel.DataAnnotations;

namespace DeathflixAPI.Models;

public class MovieCredit
{
    public int Id { get; set; }

    [Required]
    public int ActorId { get; set; }

    [Required]
    public int MovieId { get; set; }

    [Required]
    [StringLength(100)]
    public string Character { get; set; } = string.Empty;

    public string? Department { get; set; }

    [StringLength(100)]
    public string? Role { get; set; }

    [Required]
    public virtual Actor Actor { get; set; } = null!;

    [Required]
    public virtual Movie Movie { get; set; } = null!;
}