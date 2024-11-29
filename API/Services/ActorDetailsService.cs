using Microsoft.EntityFrameworkCore;
using DeathflixAPI.Data;
using DeathflixAPI.Models;
using DeathflixAPI.Models.Tmdb;

namespace DeathflixAPI.Services;

public class ActorDetailsService
{
    private readonly ILogger<ActorDetailsService> _logger;
    private readonly ITmdbService _tmdbService;
    private readonly TimeSpan _livingActorUpdateInterval = TimeSpan.FromDays(30);  // Update living actors monthly
    private readonly TimeSpan _apiRequestDelay = TimeSpan.FromMilliseconds(250);   // Rate limiting

    public ActorDetailsService(
        ILogger<ActorDetailsService> logger,
        ITmdbService tmdbService)
    {
        _logger = logger;
        _tmdbService = tmdbService;
    }

    public async Task UpdateActorDetailsAsync(Actor actor, CancellationToken cancellationToken = default)
    {
        try
        {
            // Skip if actor is deceased and we already have their details
            if (actor.DateOfDeath.HasValue && !string.IsNullOrEmpty(actor.Biography))
            {
                return;
            }

            // Skip if we checked recently and they're not deceased
            if (!actor.DateOfDeath.HasValue &&
                DateTime.UtcNow - actor.LastDetailsCheck < _livingActorUpdateInterval)
            {
                return;
            }

            _logger.LogInformation("Updating details for actor: {ActorName} (ID: {TmdbId})",
                actor.Name, actor.TmdbId);

            var details = await _tmdbService.GetActorDetailsAsync(actor.TmdbId);
            if (details != null)
            {
                actor.Biography = details.Biography;
                actor.DateOfBirth = details.Birthday != null ? DateOnly.Parse(details.Birthday) : null;
                actor.DateOfDeath = details.Deathday != null ? DateOnly.Parse(details.Deathday) : null;
                actor.PlaceOfBirth = details.PlaceOfBirth;
                actor.LastDetailsCheck = DateTime.UtcNow;

                _logger.LogInformation("Successfully updated details for actor: {ActorName}", actor.Name);
            }

            // Add delay for rate limiting
            await Task.Delay(_apiRequestDelay, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating details for actor: {ActorName} (ID: {TmdbId})",
                actor.Name, actor.TmdbId);
            throw;
        }
    }

    public async Task UpdateActorsBatchAsync(
        AppDbContext dbContext,
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;

            // Get actors that need updating
            var actorsToUpdate = await dbContext.Actors
                .Where(a =>
                    // Living actors that haven't been checked recently
                    (!a.DateOfDeath.HasValue && a.LastDetailsCheck < now - _livingActorUpdateInterval) ||
                    // New actors that haven't been checked at all
                    a.LastDetailsCheck == DateTime.MinValue)
                .OrderBy(a => a.LastDetailsCheck)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} actors requiring detail updates", actorsToUpdate.Count);

            foreach (var actor in actorsToUpdate)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    await UpdateActorDetailsAsync(actor, cancellationToken);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing actor {ActorName}, continuing with next",
                        actor.Name);
                    continue;
                }
            }

            _logger.LogInformation("Completed batch update for {Count} actors", actorsToUpdate.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch actor update");
            throw;
        }
    }
}