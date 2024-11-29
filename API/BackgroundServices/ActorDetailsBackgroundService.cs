using Microsoft.EntityFrameworkCore;
using DeathflixAPI.Data;
using DeathflixAPI.Models;
using DeathflixAPI.Services;

namespace DeathflixAPI.BackgroundServices;

public class ActorDetailsBackgroundService : BackgroundService
{
    private readonly ILogger<ActorDetailsBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _delay = TimeSpan.FromMinutes(2);
    private readonly int _batchSize = 50;
    private readonly int _timeBetweenBatches = 5;
    private readonly ActorUpdateProgress _progress = new()
    {
        StartTime = DateTime.UtcNow
    };

    public ActorDetailsBackgroundService(
        ILogger<ActorDetailsBackgroundService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Actor Details Background Service starting...");

            await Task.Delay(_delay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessActorDetailsAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Actor Details Background Service stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in Actor Details Background Service");
            throw;
        }
    }

    private async Task ProcessActorDetailsAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var actorDetailsService = scope.ServiceProvider.GetRequiredService<ActorDetailsService>();

            _progress.TotalActors = await dbContext.Actors.CountAsync(stoppingToken);
            _progress.ProcessedCount = 0;
            _progress.UpdatedCount = 0;
            _progress.StartTime = DateTime.UtcNow;

            while (!stoppingToken.IsCancellationRequested)
            {
                var actorsToUpdate = await GetNextActorBatchAsync(dbContext, stoppingToken);

                if (!actorsToUpdate.Any())
                {
                    _logger.LogInformation("No more actors need updating at this time");
                    break;
                }

                foreach (var actor in actorsToUpdate)
                {
                    try
                    {
                        _progress.CurrentActor = actor.Name;
                        _progress.ProcessedCount++;

                        bool wasUpdated = await actorDetailsService.UpdateActorDetailsAsync(actor, stoppingToken);

                        if (wasUpdated)
                        {
                            _progress.UpdatedCount++;
                            _progress.LastUpdateTime = DateTime.UtcNow;
                            await dbContext.SaveChangesAsync(stoppingToken);

                            var completionPercent = (_progress.ProcessedCount * 100.0) / _progress.TotalActors;
                            _logger.LogInformation(
                                "Updated actor: {ActorName} (ID: {ActorId}, Progress: {Completion:F1}%, Updated: {Updated}/{Total})",
                                actor.Name, actor.Id, completionPercent, _progress.UpdatedCount, _progress.TotalActors);
                        }
                        else
                        {
                            _logger.LogDebug(
                                "Skipped update for actor: {ActorName} (ID: {ActorId}, Already up to date)",
                                actor.Name, actor.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error updating details for actor {ActorName} (ID: {ActorId})",
                            actor.Name, actor.Id);
                    }
                }

                // Log batch completion with rate information
                var elapsed = DateTime.UtcNow - _progress.StartTime;
                var rate = _progress.ProcessedCount / elapsed.TotalHours;
                _logger.LogInformation(
                    "Batch complete - Progress: {Processed}/{Total} ({Percent:F1}%), Rate: {Rate:F1}/hour, Elapsed: {Elapsed:hh\\:mm\\:ss}",
                    _progress.ProcessedCount, _progress.TotalActors,
                    (_progress.ProcessedCount * 100.0) / _progress.TotalActors,
                    rate, elapsed);

                await Task.Delay(TimeSpan.FromSeconds(_timeBetweenBatches), stoppingToken);
            }

            var processingTime = DateTime.UtcNow - _progress.StartTime;
            _logger.LogInformation(
                "Completed processing cycle. Total processed: {TotalProcessed}, Updated: {TotalUpdated}, Time taken: {TimeTaken}",
                _progress.ProcessedCount, _progress.UpdatedCount, processingTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing actor details");
            throw;
        }
    }

    private async Task<List<Actor>> GetNextActorBatchAsync(AppDbContext dbContext, CancellationToken stoppingToken)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-7);

        return await dbContext.Actors
            .Where(a => a.LastDetailsCheck == DateTime.MinValue || a.LastDetailsCheck < cutoffDate)
            .OrderByDescending(a => a.Popularity)
            .ThenBy(a => a.LastDetailsCheck)
            .Take(_batchSize)
            .ToListAsync(stoppingToken);
    }
}