using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeathflixAPI.Models;

public class Movie
{
    // Primary key for the movie in our database
    public int Id { get; set; }

    // ID from TheMovieDB API - we need this to match data with the external API
    [Required]
    public int TmdbId { get; set; }

    // Basic movie information
    [Required]
    [StringLength(200)]
    public string Title { get; set; }

    // Release date is nullable because some movies might not have this info
    public DateTime? ReleaseDate { get; set; }

    // Overview is the movie's description/summary
    [StringLength(500)]
    public string? Overview { get; set; }

    // Path to the movie poster image from TMDB
    [StringLength(200)]
    public string? PosterPath { get; set; }

    // This creates the other side of the relationship with MovieCredit
    // One movie can have many credits (actors)
    public virtual ICollection<MovieCredit> Credits { get; set; } = new List<MovieCredit>();
}