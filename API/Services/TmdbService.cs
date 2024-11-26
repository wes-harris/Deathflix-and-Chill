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

    public TmdbService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TmdbService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _apiKey = configuration["Tmdb:ApiKey"] ??
            throw new ArgumentException("TMDB API key not found in configuration");

        var accessToken = configuration["Tmdb:AccessToken"] ??
            throw new ArgumentException("TMDB access token not found in configuration");

        _httpClient.BaseAddress = new Uri("https://api.themoviedb.org");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        _logger.LogInformation("TMDB Service initialized with base URL: {BaseUrl}", _httpClient.BaseAddress);
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

            var url = $"/3/search/person?api_key={_apiKey}&query={Uri.EscapeDataString(query)}&include_adult=false&language=en-US";
            var response = await _httpClient.GetAsync(url);

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

            var url = $"/3/person/{tmdbId}?api_key={_apiKey}&language=en-US";
            _logger.LogDebug("Request URL: {Url}", url);

            var response = await _httpClient.GetAsync(url);

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

            var url = $"/3/person/{tmdbId}/movie_credits?api_key={_apiKey}&language=en-US";
            var response = await _httpClient.GetAsync(url);

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

    public async Task TestConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Testing TMDB API connection...");

            var response = await _httpClient.GetAsync($"/3/configuration?api_key={_apiKey}");
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("TMDB API Response Status: {StatusCode}", (int)response.StatusCode);
            _logger.LogInformation("TMDB API Response: {Content}", content);
            _logger.LogDebug("Request URI: {Uri}", response.RequestMessage?.RequestUri);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to connect to TMDB API. Status: {StatusCode}, Content: {Content}",
                    (int)response.StatusCode, content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing TMDB API connection");
            throw;
        }
    }

    public async Task<TmdbActorResponse?> GetAllActorsAsync(int page = 1)
    {
        try
        {
            _logger.LogInformation("Fetching actors page {Page}", page);

            var url = $"/3/person/popular?api_key={_apiKey}&page={page}&language=en-US";
            _logger.LogDebug("Request URL: {Url}", url);

            var response = await _httpClient.GetAsync(url);

            await EnsureSuccessStatusCodeAsync(response);

            var result = await response.Content.ReadFromJsonAsync<TmdbActorResponse>();

            _logger.LogInformation("Found {Count} actors on page {Page}",
                result?.Results.Count ?? 0, page);

            return result;
        }
        catch (Exception ex) when (ex is not TmdbApiException)
        {
            _logger.LogError(ex, "Error fetching actors for page {Page}: {Message}", page, ex.Message);
            throw new TmdbApiException($"Failed to get actors for page {page}", ex);
        }
    }

    public Task<List<TmdbActorDetails>> GetRecentlyDeceasedActorsAsync()
    {
        _logger.LogWarning("GetRecentlyDeceasedActorsAsync not implemented");
        throw new NotImplementedException("This feature requires a custom implementation");
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
}