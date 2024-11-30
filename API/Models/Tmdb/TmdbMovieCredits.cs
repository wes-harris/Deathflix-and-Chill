using System.Text.Json.Serialization;

namespace DeathflixAPI.Models.Tmdb;

public class TmdbMovieCredits
{
    [JsonPropertyName("cast")]
    public List<TmdbMovieCastEntry> Cast { get; set; } = new();
}

public class TmdbMovieCastEntry
{
    [JsonPropertyName("id")]
    public int MovieId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("character")]
    public string Character { get; set; } = string.Empty;

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }

    [JsonPropertyName("poster_path")]
    public string? PosterPath { get; set; }
}