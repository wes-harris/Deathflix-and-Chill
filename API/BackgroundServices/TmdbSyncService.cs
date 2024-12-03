using Microsoft.EntityFrameworkCore;
using DeathflixAPI.Data;
using DeathflixAPI.Services;
using DeathflixAPI.Models;

namespace DeathflixAPI.BackgroundServices;

public class TmdbSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TmdbSyncService> _logger;
    private readonly TimeSpan _syncInterval = TimeSpan.FromHours(24);
    private readonly TimeSpan _deathCheckInterval = TimeSpan.FromDays(7);

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

                // Process daily export file
                await ProcessDailyExportAsync(stoppingToken);

                // Check for deaths among existing actors
                await CheckForDeathsAsync(stoppingToken);

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

    private async Task ProcessDailyExportAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var exportService = scope.ServiceProvider.GetRequiredService<TmdbExportService>();

        await exportService.ProcessDailyExportAsync(scope.ServiceProvider, stoppingToken);
    }

    private async Task CheckForDeathsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tmdbService = scope.ServiceProvider.GetRequiredService<ITmdbService>();

        var checkBeforeDate = DateTime.UtcNow.Subtract(_deathCheckInterval);

        // Get actors that haven't been checked recently and aren't known to be deceased
        var actorsToCheck = await dbContext.Actors
            .Where(a => a.LastDeathCheck < checkBeforeDate && !a.DateOfDeath.HasValue)
            .OrderBy(a => a.LastDeathCheck)
            .Take(100)  // Process in batches to avoid overwhelming the API
            .ToListAsync(stoppingToken);

        foreach (var actor in actorsToCheck)
        {
            try
            {
                _logger.LogInformation("Checking death status for actor: {ActorName} (ID: {TmdbId})",
                    actor.Name, actor.TmdbId);

                var details = await tmdbService.GetActorDetailsAsync(actor.TmdbId);
                if (details?.Deathday != null)
                {
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

                actor.LastDeathCheck = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(stoppingToken);

                // Add a small delay between API calls
                await Task.Delay(500, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking death status for actor {ActorName} (ID: {TmdbId})",
                    actor.Name, actor.TmdbId);
                continue;
            }
        }
    }
}