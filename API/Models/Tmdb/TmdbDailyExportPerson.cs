using System.Text.Json.Serialization;

namespace DeathflixAPI.Models.Tmdb;

public class TmdbDailyExportPerson
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("adult")]
    public bool Adult { get; set; }

    [JsonPropertyName("popularity")]
    public double Popularity { get; set; }
}