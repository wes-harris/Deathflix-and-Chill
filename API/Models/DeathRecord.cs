using System.ComponentModel.DataAnnotations;

namespace DeathflixAPI.Models;

public class DeathRecord
{
    public int Id { get; set; }

    public int ActorId { get; set; }
    public virtual Actor Actor { get; set; } = null!;

    [Required]
    public DateTime DateOfDeath { get; set; }

    [MaxLength(500)]
    public string? CauseOfDeath { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [MaxLength(1000)]
    public string? AdditionalDetails { get; set; }

    // Source verification
    [MaxLength(500)]
    public string? SourceUrl { get; set; }

    public bool IsVerified { get; set; }

    // Audit properties
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}