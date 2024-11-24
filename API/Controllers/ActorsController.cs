using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeathflixAPI.Data;
using DeathflixAPI.Models;
using DeathflixAPI.Services;
using DeathflixAPI.Exceptions;

namespace DeathflixAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActorsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ActorsController> _logger;
    private readonly ITmdbService _tmdbService;

    public ActorsController(
        AppDbContext context,
        ILogger<ActorsController> logger,
        ITmdbService tmdbService)
    {
        _context = context;
        _logger = logger;
        _tmdbService = tmdbService;
    }

    // GET: api/actors
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Actor>>> GetActors()
    {
        try
        {
            _logger.LogInformation("Retrieving all actors");
            return await _context.Actors.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving actors");
            return StatusCode(500, "An error occurred while retrieving actors");
        }
    }

    // GET: api/actors/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Actor>> GetActor(int id)
    {
        try
        {
            _logger.LogInformation("Retrieving actor with ID: {Id}", id);

            var actor = await _context.Actors
                .Include(a => a.DeathRecord)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (actor == null)
            {
                _logger.LogWarning("Actor with ID {Id} not found", id);
                return NotFound($"Actor with ID {id} not found");
            }

            return actor;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving actor with ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the actor");
        }
    }

    // POST: api/actors
    [HttpPost]
    public async Task<ActionResult<Actor>> CreateActor(Actor actor)
    {
        try
        {
            if (actor == null)
            {
                return BadRequest("Actor data is required");
            }

            _logger.LogInformation("Creating new actor: {ActorName}", actor.Name);

            _context.Actors.Add(actor);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully created actor with ID: {ActorId}", actor.Id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while saving actor: {Message}", ex.InnerException?.Message ?? ex.Message);
                return StatusCode(500, $"Database error occurred while saving actor: {ex.InnerException?.Message ?? ex.Message}");
            }

            return CreatedAtAction(
                nameof(GetActor),
                new { id = actor.Id },
                actor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating actor: {Message}", ex.Message);
            return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
        }
    }

    // PUT: api/actors/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateActor(int id, Actor actor)
    {
        try
        {
            if (id != actor.Id)
            {
                _logger.LogWarning("Mismatched ID in UpdateActor request");
                return BadRequest("ID mismatch");
            }

            _context.Entry(actor).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException dbEx)  // Changed variable name to dbEx and used it
            {
                _logger.LogWarning(dbEx, "Concurrency issue occurred while updating actor with ID: {Id}", id);
                if (!ActorExists(id))
                {
                    _logger.LogWarning("Actor with ID {Id} not found during update", id);
                    return NotFound($"Actor with ID {id} not found");
                }
                throw;
            }

            _logger.LogInformation("Successfully updated actor with ID: {Id}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating actor with ID: {Id}", id);
            return StatusCode(500, "An error occurred while updating the actor");
        }
    }

    // DELETE: api/actors/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteActor(int id)
    {
        try
        {
            var actor = await _context.Actors.FindAsync(id);
            if (actor == null)
            {
                _logger.LogWarning("Actor with ID {Id} not found for deletion", id);
                return NotFound($"Actor with ID {id} not found");
            }

            _context.Actors.Remove(actor);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully deleted actor with ID: {Id}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting actor with ID: {Id}", id);
            return StatusCode(500, "An error occurred while deleting the actor");
        }
    }

    // GET: api/actors/search
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Actor>>> SearchActors([FromQuery] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query is required");
            }

            _logger.LogInformation("Searching for actors with query: {Query}", query);

            var searchResult = await _tmdbService.SearchActorsAsync(query);
            if (searchResult?.Results == null || !searchResult.Results.Any())
            {
                return NotFound("No actors found matching the search criteria");
            }

            // Here you would typically convert TMDB results to your Actor model
            // This is a simplified example
            var actors = searchResult.Results.Select(r => new Actor
            {
                TmdbId = r.Id,
                Name = r.Name,
                ProfileImagePath = r.ProfilePath
            });

            return Ok(actors);
        }
        catch (TmdbApiException ex) when (ex.StatusCode == 429)
        {
            _logger.LogWarning(ex, "TMDB API rate limit exceeded");
            return StatusCode(503, "Search service is temporarily unavailable. Please try again later.");
        }
        catch (TmdbApiException ex)
        {
            _logger.LogError(ex, "TMDB API error occurred during search");
            return StatusCode(502, "Error communicating with search service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during actor search");
            return StatusCode(500, "An unexpected error occurred");
        }
    }

    private bool ActorExists(int id)
    {
        return _context.Actors.Any(e => e.Id == id);
    }
}