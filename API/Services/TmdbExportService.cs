using System.IO.Compression;
using System.Text.Json;
using DeathflixAPI.Models.Tmdb;
using DeathflixAPI.Data;
using Microsoft.EntityFrameworkCore;
using DeathflixAPI.Models;

namespace DeathflixAPI.Services;

public class TmdbExportService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TmdbExportService> _logger;
    private readonly string _downloadPath;
    private readonly string _apiKey;
    private const string BaseUrl = "http://files.tmdb.org/p/exports/";
    private const double PopularityThreshold = 5.0;

    public TmdbExportService(
        HttpClient httpClient,
        ILogger<TmdbExportService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _downloadPath = Path.Combine(Path.GetTempPath(), "tmdb_exports");
        _apiKey = configuration["Tmdb:ApiKey"] ??
            throw new ArgumentException("TMDB API key not found in configuration");

        if (!Directory.Exists(_downloadPath))
        {
            Directory.CreateDirectory(_downloadPath);
        }
    }

    private async Task<string?> GetLatestExportFileNameAsync()
    {
        try
        {
            var potentialFiles = new[]
            {
                $"person_ids_{DateTime.UtcNow:MM_dd_yyyy}.json.gz",
                "person_ids_daily.json.gz",
                "people_daily.json.gz"
            };

            foreach (var fileName in potentialFiles)
            {
                var testUrl = GetExportUrl(fileName);
                _logger.LogInformation("Checking for file at: {Url}", testUrl);

                var response = await _httpClient.GetAsync(testUrl);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Found available export file: {FileName}", fileName);
                    return fileName;
                }
                _logger.LogInformation("File not found: {FileName} (Status: {Status})",
                    fileName, response.StatusCode);
            }

            _logger.LogError("No export files found");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting export file listing");
            return null;
        }
    }

    public async Task ProcessDailyExportAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        try
        {
            await using var initialScope = serviceProvider.CreateAsyncScope();
            var dbContext = initialScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var actorDetailsService = serviceProvider.GetRequiredService<ActorDetailsService>();

            var currentActorCount = await dbContext.Actors.CountAsync(cancellationToken);
            _logger.LogInformation("Current actor count in database: {Count}", currentActorCount);

            await ProcessBasicActorDataAsync(serviceProvider, cancellationToken);
            await actorDetailsService.UpdateActorsBatchAsync(dbContext, 100, cancellationToken);

            _logger.LogInformation("Completed daily actor sync and detail updates");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing daily export and updates");
            throw;
        }
    }

    private async Task ProcessBasicActorDataAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var fileName = await GetLatestExportFileNameAsync();
        if (fileName == null)
        {
            _logger.LogError("Could not determine export file name");
            return;
        }

        _logger.LogInformation("Attempting to download file: {FileName}", fileName);
        var filePath = await DownloadExportFileAsync(fileName);

        if (filePath == null)
        {
            _logger.LogError("Failed to download export file");
            return;
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await using var fileStream = File.OpenRead(filePath);
        await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream);

        string? line;
        int processedCount = 0;
        int skippedCount = 0;
        int newActorsCount = 0;
        int updatedActorsCount = 0;
        var batchSize = 100;

        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var person = JsonSerializer.Deserialize<TmdbDailyExportPerson>(line);
            if (person == null) continue;

            // Skip actors below the popularity threshold
            if (person.Popularity < PopularityThreshold)
            {
                skippedCount++;
                if (skippedCount % 10000 == 0)
                {
                    _logger.LogInformation("Skipped {Count} low-popularity actors", skippedCount);
                }
                continue;
            }

            var actor = await dbContext.Actors
                .FirstOrDefaultAsync(a => a.TmdbId == person.Id, cancellationToken);

            if (actor == null)
            {
                actor = new Actor
                {
                    TmdbId = person.Id,
                    Name = person.Name,
                    Popularity = person.Popularity,
                    LastDetailsCheck = DateTime.MinValue,
                    LastDeathCheck = DateTime.MinValue
                };
                dbContext.Actors.Add(actor);
                newActorsCount++;
                _logger.LogInformation("Adding new actor: {ActorName} (TMDB ID: {TmdbId}, Popularity: {Popularity})",
                    person.Name, person.Id, person.Popularity);
            }
            else if (actor.Name != person.Name || Math.Abs(actor.Popularity - person.Popularity) > 0.001)
            {
                actor.Name = person.Name;
                actor.Popularity = person.Popularity;
                dbContext.Actors.Update(actor);
                updatedActorsCount++;
                _logger.LogInformation("Updating actor: {ActorName} (TMDB ID: {TmdbId}, Popularity: {Popularity})",
                    person.Name, person.Id, person.Popularity);
            }

            processedCount++;
            if (processedCount % batchSize == 0)
            {
                _logger.LogInformation(
                    "Processed {Count} actors (New: {NewCount}, Updated: {UpdatedCount}, Skipped: {SkippedCount})",
                    processedCount, newActorsCount, updatedActorsCount, skippedCount);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        if (processedCount % batchSize != 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Clean up low-popularity actors that are no longer relevant
        var lowPopularityActors = await dbContext.Actors
            .Where(a => a.Popularity < PopularityThreshold)
            .ToListAsync(cancellationToken);

        if (lowPopularityActors.Any())
        {
            _logger.LogInformation("Removing {Count} actors that fell below popularity threshold",
                lowPopularityActors.Count);
            dbContext.Actors.RemoveRange(lowPopularityActors);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var finalActorCount = await dbContext.Actors.CountAsync(cancellationToken);
        _logger.LogInformation(
            "Completed processing daily export. Total processed: {Count}, New: {NewCount}, Updated: {UpdatedCount}, Skipped: {SkippedCount}, Final DB Count: {FinalCount}",
            processedCount, newActorsCount, updatedActorsCount, skippedCount, finalActorCount);

        try
        {
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete export file: {FilePath}", filePath);
        }
    }

    private string GetExportUrl(string fileName)
    {
        return $"https://files.tmdb.org/p/exports/{fileName}";
    }

    private async Task<string?> DownloadExportFileAsync(string fileName)
    {
        try
        {
            var url = GetExportUrl(fileName);
            var filePath = Path.Combine(_downloadPath, fileName);

            _logger.LogInformation("Downloading export file from: {Url}", url);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to download export file. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            await using var fileStream = File.Create(filePath);
            await using var downloadStream = await response.Content.ReadAsStreamAsync();
            await downloadStream.CopyToAsync(fileStream);

            _logger.LogInformation("Successfully downloaded export file to: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading export file");
            return null;
        }
    }

    public async Task TestExportAccessAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(BaseUrl);
            _logger.LogInformation("Export service response: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Export service content: {Content}", content);
            }

            var testUrl = GetExportUrl("person_ids_daily.json.gz");
            _logger.LogInformation("Testing specific file access: {Url}", testUrl);

            var fileResponse = await _httpClient.GetAsync(testUrl);
            _logger.LogInformation("File access response: {StatusCode}", fileResponse.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing export access");
            throw;
        }
    }
}