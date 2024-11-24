using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DeathflixAPI.Models.Tmdb;
using DeathflixAPI.Exceptions;

namespace DeathflixAPI.Services;

public class TmdbService : ITmdbService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<TmdbService> _logger;
    private const string BaseUrl = "https://api.themoviedb.org/3";

    public TmdbService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TmdbService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Tmdb:ApiKey"] ??
            throw new ArgumentNullException("Tmdb:ApiKey", "TMDB API key not found in configuration");

        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    public async Task<TmdbActorResponse?> SearchActorsAsync(string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Search query cannot be empty", nameof(query));
            }

            _logger.LogInformation("Searching for actors with query: {Query}", query);

            var response = await _httpClient.GetAsync(
                $"/search/person?api_key={_apiKey}&query={Uri.EscapeDataString(query)}&include_adult=false");

            await EnsureSuccessStatusCodeAsync(response);

            var result = await response.Content.ReadFromJsonAsync<TmdbActorResponse>();

            _logger.LogInformation("Found {Count} actors matching query: {Query}",
                result?.Results.Count ?? 0, query);

            return result;
        }
        catch (Exception ex) when (ex is not TmdbApiException)
        {
            _logger.LogError(ex, "Error searching for actors with query: {Query}", query);
            throw new TmdbApiException("Failed to search for actors", ex);
        }
    }

    public async Task<TmdbActorDetails?> GetActorDetailsAsync(int tmdbId)
    {
        try
        {
            if (tmdbId <= 0)
            {
                throw new ArgumentException("Invalid TMDB ID", nameof(tmdbId));
            }

            _logger.LogInformation("Fetching details for actor with TMDB ID: {TmdbId}", tmdbId);

            var response = await _httpClient.GetAsync($"/person/{tmdbId}?api_key={_apiKey}");

            await EnsureSuccessStatusCodeAsync(response);

            var result = await response.Content.ReadFromJsonAsync<TmdbActorDetails>();

            _logger.LogInformation("Successfully retrieved details for actor: {ActorName}",
                result?.Name ?? "Unknown");

            return result;
        }
        catch (Exception ex) when (ex is not TmdbApiException)
        {
            _logger.LogError(ex, "Error fetching actor details for TMDB ID: {TmdbId}", tmdbId);
            throw new TmdbApiException($"Failed to get actor details for ID: {tmdbId}", ex);
        }
    }

    public async Task<TmdbMovieCredits?> GetActorMovieCreditsAsync(int tmdbId)
    {
        try
        {
            if (tmdbId <= 0)
            {
                throw new ArgumentException("Invalid TMDB ID", nameof(tmdbId));
            }

            _logger.LogInformation("Fetching movie credits for actor with TMDB ID: {TmdbId}", tmdbId);

            var response = await _httpClient.GetAsync(
                $"/person/{tmdbId}/movie_credits?api_key={_apiKey}");

            await EnsureSuccessStatusCodeAsync(response);

            var result = await response.Content.ReadFromJsonAsync<TmdbMovieCredits>();

            _logger.LogInformation("Found {Count} movie credits for actor with TMDB ID: {TmdbId}",
                result?.Cast.Count ?? 0, tmdbId);

            return result;
        }
        catch (Exception ex) when (ex is not TmdbApiException)
        {
            _logger.LogError(ex, "Error fetching movie credits for TMDB ID: {TmdbId}", tmdbId);
            throw new TmdbApiException($"Failed to get movie credits for actor ID: {tmdbId}", ex);
        }
    }

    private async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogError("TMDB API error: {StatusCode} - {Content}",
                (int)response.StatusCode, content);

            throw new TmdbApiException(
                $"TMDB API returned error {(int)response.StatusCode}: {content}",
                (int)response.StatusCode);
        }
    }

    public Task<List<TmdbActorDetails>> GetRecentlyDeceasedActorsAsync()
    {
        // Still to be implemented
        _logger.LogWarning("GetRecentlyDeceasedActorsAsync not implemented");
        throw new NotImplementedException("This feature requires a custom implementation");
    }
}