using System;
using System.ComponentModel.DataAnnotations;

namespace DeathflixAPI.Models;

public class DeathRecord
{
    public int Id { get; set; }

    [Required]
    public int ActorId { get; set; }

    [Required]
    public DateTime DateOfDeath { get; set; }

    [StringLength(200)]
    public string? CauseOfDeath { get; set; }

    [StringLength(200)]
    public string? PlaceOfDeath { get; set; }

    [StringLength(1000)]
    public string? AdditionalDetails { get; set; }

    public DateTime LastVerified { get; set; }

    [StringLength(500)]
    public string? SourceUrl { get; set; }

    [Required]
    public virtual Actor Actor { get; set; } = null!;
}