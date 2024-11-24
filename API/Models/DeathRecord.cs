using System;
using System.ComponentModel.DataAnnotations;

namespace DeathflixAPI.Models;

public class DeathRecord
{
    // Primary key for the death record
    public int Id { get; set; }

    // Foreign key to link to the Actor
    // Each death record must be connected to an actor
    [Required]
    public int ActorId { get; set; }

    // When the actor passed away
    [Required]
    public DateTime DateOfDeath { get; set; }

    // Optional details about the death
    [StringLength(200)]
    public string? CauseOfDeath { get; set; }

    [StringLength(200)]
    public string? PlaceOfDeath { get; set; }

    // Any additional information about their passing
    [StringLength(1000)]
    public string? AdditionalDetails { get; set; }

    // Metadata for tracking when we last verified this information
    public DateTime LastVerified { get; set; }

    // URL to the source of this information
    [StringLength(500)]
    public string? SourceUrl { get; set; }

    // Navigation property - links back to the Actor
    // This creates a one-to-one relationship with Actor
    public virtual Actor Actor { get; set; }
}