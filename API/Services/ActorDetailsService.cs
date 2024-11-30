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

    public async Task<bool> UpdateActorDetailsAsync(Actor actor, CancellationToken cancellationToken = default)
    {
        try
        {
            // Skip if actor is deceased and we already have their complete details
            if (actor.DateOfDeath.HasValue &&
                !string.IsNullOrEmpty(actor.Biography) &&
                !string.IsNullOrEmpty(actor.ProfileImagePath))
            {
                return false;
            }

            // Skip if we checked recently and they're not deceased
            if (!actor.DateOfDeath.HasValue &&
                DateTime.UtcNow - actor.LastDetailsCheck < _livingActorUpdateInterval)
            {
                return false;
            }

            _logger.LogInformation("Updating details for actor: {ActorName} (ID: {TmdbId})",
                actor.Name, actor.TmdbId);

            var details = await _tmdbService.GetActorDetailsAsync(actor.TmdbId);
            if (details != null)
            {
                var wasUpdated = false;

                // Update biography if empty or changed
                if (string.IsNullOrEmpty(actor.Biography) || actor.Biography != details.Biography)
                {
                    actor.Biography = details.Biography;
                    wasUpdated = true;
                }

                // Update birth date if available
                if (details.Birthday != null)
                {
                    var newBirthDate = DateOnly.Parse(details.Birthday);
                    if (actor.DateOfBirth != newBirthDate)
                    {
                        actor.DateOfBirth = newBirthDate;
                        wasUpdated = true;
                    }
                }

                // Update death date if available
                if (details.Deathday != null)
                {
                    var newDeathDate = DateOnly.Parse(details.Deathday);
                    if (actor.DateOfDeath != newDeathDate)
                    {
                        actor.DateOfDeath = newDeathDate;
                        wasUpdated = true;
                    }
                }

                // Update place of birth if available
                if (!string.IsNullOrEmpty(details.PlaceOfBirth) &&
                    actor.PlaceOfBirth != details.PlaceOfBirth)
                {
                    actor.PlaceOfBirth = details.PlaceOfBirth;
                    wasUpdated = true;
                }

                // Update profile image if available
                if (!string.IsNullOrEmpty(details.ProfilePath) &&
                    actor.ProfileImagePath != details.ProfilePath)
                {
                    actor.ProfileImagePath = details.ProfilePath;
                    wasUpdated = true;
                }

                // Always update timestamps
                actor.LastDetailsCheck = DateTime.UtcNow;
                actor.LastDeathCheck = DateTime.UtcNow;

                if (wasUpdated)
                {
                    _logger.LogInformation("Successfully updated details for actor: {ActorName}", actor.Name);
                }
                else
                {
                    _logger.LogInformation("No new information available for actor: {ActorName}", actor.Name);
                }

                // Add delay for rate limiting
                await Task.Delay(_apiRequestDelay, cancellationToken);
                return wasUpdated;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating details for actor: {ActorName} (ID: {TmdbId})",
                actor.Name, actor.TmdbId);
            throw;
        }
    }

    public async Task<(int ProcessedCount, int UpdatedCount)> UpdateActorsBatchAsync(
        AppDbContext dbContext,
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            int processedCount = 0;
            int updatedCount = 0;

            var actorsToUpdate = await dbContext.Actors
                .Where(a =>
                    // Living actors that haven't been checked recently
                    (!a.DateOfDeath.HasValue && a.LastDetailsCheck < now - _livingActorUpdateInterval) ||
                    // New actors that haven't been checked at all
                    a.LastDetailsCheck == DateTime.MinValue)
                .OrderByDescending(a => a.Popularity)  // Process most popular actors first
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} actors requiring detail updates", actorsToUpdate.Count);

            foreach (var actor in actorsToUpdate)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    processedCount++;
                    bool wasUpdated = await UpdateActorDetailsAsync(actor, cancellationToken);
                    if (wasUpdated)
                    {
                        updatedCount++;
                        await dbContext.SaveChangesAsync(cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing actor {ActorName}, continuing with next",
                        actor.Name);
                    continue;
                }
            }

            _logger.LogInformation(
                "Completed batch update. Processed: {Processed}, Updated: {Updated}",
                processedCount, updatedCount);

            return (processedCount, updatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch actor update");
            throw;
        }
    }
}