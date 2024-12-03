using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeathflixAPI.Models;

public class Movie
{
    public int Id { get; set; }

    [Required]
    public int TmdbId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public DateTime? ReleaseDate { get; set; }

    [StringLength(500)]
    public string? Overview { get; set; }

    [StringLength(200)]
    public string? PosterPath { get; set; }

    public virtual ICollection<MovieCredit> Credits { get; set; } = new List<MovieCredit>();
}