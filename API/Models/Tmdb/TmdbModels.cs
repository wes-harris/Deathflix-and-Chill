using System.Text.Json.Serialization;

namespace DeathflixAPI.Models.Tmdb;

public class TmdbActorResponse
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("results")]
    public List<TmdbActorResult> Results { get; set; } = new();

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("total_results")]
    public int TotalResults { get; set; }
}

public class TmdbActorResult
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("profile_path")]
    public string? ProfilePath { get; set; }

    [JsonPropertyName("known_for_department")]
    public string? KnownForDepartment { get; set; }
}

public class TmdbActorDetails
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("birthday")]
    public string? Birthday { get; set; }

    [JsonPropertyName("deathday")]
    public string? Deathday { get; set; }

    [JsonPropertyName("biography")]
    public string? Biography { get; set; }

    [JsonPropertyName("place_of_birth")]
    public string? PlaceOfBirth { get; set; }

    [JsonPropertyName("profile_path")]
    public string? ProfilePath { get; set; }
}

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
}