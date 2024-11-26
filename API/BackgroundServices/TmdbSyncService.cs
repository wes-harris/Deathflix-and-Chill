using Microsoft.EntityFrameworkCore;
using DeathflixAPI.Data;
using DeathflixAPI.Services;
using DeathflixAPI.Models;
using DeathflixAPI.Models.Tmdb;

namespace DeathflixAPI.BackgroundServices;

public class TmdbSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TmdbSyncService> _logger;
    private readonly TimeSpan _syncInterval = TimeSpan.FromHours(24);

    public TmdbSyncService(
        IServiceScopeFactory scopeFactory,
        ILogger<TmdbSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting TMDB sync at: {time}", DateTimeOffset.Now);
                await SyncTmdbDataAsync(stoppingToken);
                _logger.LogInformation("TMDB sync completed at: {time}", DateTimeOffset.Now);

                // Wait for the next sync interval
                await Task.Delay(_syncInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while syncing TMDB data");
                // Wait a shorter time if there's an error before retrying
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }

    private async Task SyncTmdbDataAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var tmdbService = scope.ServiceProvider.GetRequiredService<ITmdbService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SyncAllActors(tmdbService, dbContext, stoppingToken);
        await SyncDeathInformation(tmdbService, dbContext, stoppingToken);
        await dbContext.SaveChangesAsync(stoppingToken);
    }

    private async Task SyncAllActors(ITmdbService tmdbService, AppDbContext dbContext, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting full actor database sync");

            // Get first page to determine total pages
            var firstPage = await tmdbService.GetAllActorsAsync(1);
            if (firstPage == null)
            {
                _logger.LogWarning("No actor data received");
                return;
            }

            // Process first page
            await ProcessActorPage(tmdbService, firstPage.Results, dbContext);

            // Process all remaining pages
            for (int page = 2; page <= firstPage.TotalPages; page++)
            {
                if (stoppingToken.IsCancellationRequested) break;

                _logger.LogInformation("Processing page {CurrentPage} of {TotalPages}", page, firstPage.TotalPages);

                var pageResult = await tmdbService.GetAllActorsAsync(page);
                if (pageResult?.Results != null)
                {
                    await ProcessActorPage(tmdbService, pageResult.Results, dbContext);
                }

                // Save every 5 pages to avoid memory issues
                if (page % 5 == 0)
                {
                    await dbContext.SaveChangesAsync(stoppingToken);
                }

                // Add a delay to avoid rate limits (TMDB allows 40 requests per 10 seconds)
                await Task.Delay(250, stoppingToken); // 250ms delay = ~4 requests per second
            }

            await dbContext.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Completed full actor database sync");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during actor sync");
            throw;
        }
    }

    private async Task ProcessActorPage(ITmdbService tmdbService, List<TmdbActorResult> actors, AppDbContext dbContext)
    {
        foreach (var actorData in actors)
        {
            try
            {
                // Check if actor exists
                var existingActor = await dbContext.Actors
                    .FirstOrDefaultAsync(a => a.TmdbId == actorData.Id);

                if (existingActor == null)
                {
                    // New actor - fetch full details and create
                    var details = await tmdbService.GetActorDetailsAsync(actorData.Id);
                    if (details != null)
                    {
                        var actor = new Actor
                        {
                            TmdbId = details.Id,
                            Name = details.Name,
                            Biography = details.Biography,
                            DateOfBirth = details.Birthday != null ? DateOnly.Parse(details.Birthday) : null,
                            DateOfDeath = details.Deathday != null ? DateOnly.Parse(details.Deathday) : null,
                            PlaceOfBirth = details.PlaceOfBirth,
                            ProfileImagePath = details.ProfilePath
                        };
                        dbContext.Actors.Add(actor);
                        _logger.LogInformation("Added new actor: {ActorName} (TMDB ID: {TmdbId})",
                            actor.Name, actor.TmdbId);
                    }
                }
                else
                {
                    // Existing actor - check for updates
                    var details = await tmdbService.GetActorDetailsAsync(actorData.Id);
                    if (details != null)
                    {
                        existingActor.Name = details.Name;
                        existingActor.Biography = details.Biography;
                        existingActor.DateOfBirth = details.Birthday != null ? DateOnly.Parse(details.Birthday) : null;
                        existingActor.DateOfDeath = details.Deathday != null ? DateOnly.Parse(details.Deathday) : null;
                        existingActor.PlaceOfBirth = details.PlaceOfBirth;
                        existingActor.ProfileImagePath = details.ProfilePath;

                        _logger.LogInformation("Updated actor: {ActorName} (TMDB ID: {TmdbId})",
                            existingActor.Name, existingActor.TmdbId);
                    }
                }

                // Add a small delay between individual actor detail requests
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing actor with TMDB ID: {TmdbId}", actorData.Id);
                // Continue processing other actors even if one fails
                continue;
            }
        }
    }

    private async Task SyncDeathInformation(ITmdbService tmdbService, AppDbContext dbContext, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting death information sync");

            // Get all actors without death records
            var actorsToCheck = await dbContext.Actors
                .Where(a => a.DeathRecord == null && a.DateOfDeath == null)
                .ToListAsync(stoppingToken);

            foreach (var actor in actorsToCheck)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    var details = await tmdbService.GetActorDetailsAsync(actor.TmdbId);
                    if (details?.Deathday != null)
                    {
                        // Update actor death information
                        actor.DateOfDeath = DateOnly.Parse(details.Deathday);

                        // Create death record
                        var deathRecord = new DeathRecord
                        {
                            ActorId = actor.Id,
                            DateOfDeath = actor.DateOfDeath.Value,
                            LastVerified = DateTime.UtcNow
                        };

                        dbContext.DeathRecords.Add(deathRecord);
                        _logger.LogInformation("Added death record for actor: {ActorName}", actor.Name);
                    }

                    // Add a small delay to avoid hitting rate limits
                    await Task.Delay(100, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking death information for actor {ActorName} (ID: {ActorId})",
                        actor.Name, actor.Id);
                    continue;
                }
            }

            await dbContext.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Completed death information sync");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during death information sync");
            throw;
        }
    }
}