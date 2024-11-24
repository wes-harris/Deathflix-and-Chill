using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeathflixAPI.Models;

public class Actor
{
    public int Id { get; set; }

    [Required]
    public int TmdbId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Biography { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTime? DateOfBirth { get; set; }

    [Column(TypeName = "timestamptz")]
    public DateTime? DateOfDeath { get; set; }

    [StringLength(200)]
    public string? PlaceOfBirth { get; set; }

    [StringLength(200)]
    public string? ProfileImagePath { get; set; }

    public bool IsDeceased => DateOfDeath.HasValue;

    public virtual DeathRecord? DeathRecord { get; set; }
    public virtual ICollection<MovieCredit> MovieCredits { get; set; } = new List<MovieCredit>();
}