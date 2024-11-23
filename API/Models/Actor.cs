using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeathflixAPI.Models;

public class Actor
{
    // This is the primary key - Entity Framework will recognize 'Id' as the primary key by convention
    public int Id { get; set; }

    // This will store the ID from TheMovieDB API
    // [Required] means this field cannot be null in the database
    [Required]
    public int TmdbId { get; set; }

    // [StringLength(200)] limits the string to 200 characters
    [Required]
    [StringLength(200)]
    public string Name { get; set; }

    // The ? after string makes this property nullable
    [StringLength(500)]
    public string? Biography { get; set; }

    // DateTime? means this is a nullable date
    public DateTime? DateOfBirth { get; set; }
    public DateTime? DateOfDeath { get; set; }

    [StringLength(200)]
    public string? PlaceOfBirth { get; set; }

    [StringLength(200)]
    public string? ProfileImagePath { get; set; }

    // This is a computed property - it's not stored in the database
    // It returns true if DateOfDeath has a value
    public bool IsDeceased => DateOfDeath.HasValue;

    // These are navigation properties - they help Entity Framework set up relationships
    // virtual enables lazy loading, meaning the related data is only loaded when accessed
    public virtual DeathRecord? DeathRecord { get; set; }
    public virtual ICollection<MovieCredit> MovieCredits { get; set; } = new List<MovieCredit>();
}