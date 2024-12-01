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
    public async Task<ActionResult<PagedResponse<Actor>>> GetActors([FromQuery] PaginationParameters parameters)
    {
        try
        {
            _logger.LogInformation("Retrieving actors with pagination. Page: {PageNumber}, Size: {PageSize}",
                parameters.PageNumber, parameters.PageSize);

            var query = _context.Actors.AsQueryable();

            var totalRecords = await query.CountAsync();
            var actors = await query
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .ToListAsync();

            var pagedResponse = new PagedResponse<Actor>
            {
                Data = actors,
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)parameters.PageSize)
            };

            return Ok(pagedResponse);
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

    // GET: api/actors/{id}/details
    [HttpGet("{id}/details")]
    public async Task<ActionResult<dynamic>> GetActorDetails(int id)
    {
        try
        {
            var actor = await _context.Actors
                .Include(a => a.DeathRecord)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (actor == null)
            {
                _logger.LogWarning("Actor with ID {Id} not found in database", id);
                return NotFound($"Actor with ID {id} not found");
            }

            try
            {
                // Get additional details from TMDB
                var tmdbDetails = await _tmdbService.GetActorDetailsAsync(actor.TmdbId);

                // Combine local and TMDB data
                var result = new
                {
                    actor.Id,
                    actor.TmdbId,
                    actor.Name,
                    Biography = tmdbDetails?.Biography,
                    BirthDate = tmdbDetails?.Birthday,
                    DeathDate = actor.DateOfDeath,
                    PlaceOfBirth = tmdbDetails?.PlaceOfBirth,
                    ProfileImagePath = tmdbDetails?.ProfilePath,
                    DeathRecord = actor.DeathRecord,
                    IsDeceased = actor.IsDeceased
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching TMDB details for actor {Id}", id);
                // Return basic actor info if TMDB fetch fails
                return Ok(actor);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving actor details for ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving actor details");
        }
    }

    // GET: api/actors/{id}/credits
    [HttpGet("{id}/credits")]
    public async Task<ActionResult<dynamic>> GetActorCredits(int id)
    {
        try
        {
            var actor = await _context.Actors.FindAsync(id);
            if (actor == null)
            {
                return NotFound($"Actor with ID {id} not found");
            }

            var credits = await _tmdbService.GetActorMovieCreditsAsync(actor.TmdbId);
            return Ok(credits);
        }
        catch (TmdbApiException ex)
        {
            _logger.LogError(ex, "TMDB API error occurred while fetching credits for actor {Id}", id);
            return StatusCode(400, "Error retrieving actor credits");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credits for actor {Id}", id);
            return StatusCode(500, "An error occurred while retrieving actor credits");
        }
    }

    [HttpGet("deceased")]
    public async Task<ActionResult<PagedResponse<dynamic>>> GetRecentlyDeceasedActors(
        [FromQuery] DateTime? since = null,
        [FromQuery] DateTime? until = null,
        [FromQuery] PaginationParameters parameters = null)
    {
        try
        {
            parameters ??= new PaginationParameters();
            _logger.LogInformation("Retrieving recently deceased actors");

            var query = _context.Actors
                .Include(a => a.DeathRecord)
                .Where(a => a.DateOfDeath.HasValue)
                .AsQueryable();

            if (since.HasValue)
            {
                var sinceDate = DateOnly.FromDateTime(since.Value);
                query = query.Where(a => a.DateOfDeath.HasValue && a.DateOfDeath.Value >= sinceDate);
            }

            if (until.HasValue)
            {
                var untilDate = DateOnly.FromDateTime(until.Value);
                query = query.Where(a => a.DateOfDeath.HasValue && a.DateOfDeath.Value <= untilDate);
            }

            var totalRecords = await query.CountAsync();

            var deceasedActors = await query
                .OrderByDescending(a => a.DateOfDeath)
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(a => new
                {
                    a.Id,
                    a.TmdbId,
                    a.Name,
                    a.ProfileImagePath,
                    DateOfBirth = a.DateOfBirth,
                    DateOfDeath = a.DateOfDeath,
                    DeathRecord = a.DeathRecord == null ? null : new
                    {
                        CauseOfDeath = a.DeathRecord.CauseOfDeath,
                        PlaceOfDeath = a.DeathRecord.PlaceOfDeath,
                        AdditionalDetails = a.DeathRecord.AdditionalDetails,
                        SourceUrl = a.DeathRecord.SourceUrl,
                        LastVerified = a.DeathRecord.LastVerified
                    }
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<dynamic>
            {
                Data = deceasedActors,
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)parameters.PageSize)
            };

            if (!deceasedActors.Any())
            {
                return NotFound("No deceased actors found in the specified time range");
            }

            return Ok(pagedResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recently deceased actors");
            return StatusCode(500, "An error occurred while retrieving deceased actors");
        }
    }

    // Search endpoint already has pagination through TMDB API, but let's make it consistent
    [HttpGet("search")]
    public async Task<ActionResult<PagedResponse<dynamic>>> SearchActors(
        [FromQuery] string query,
        [FromQuery] PaginationParameters parameters = null,
        [FromQuery] double? minPopularity = 5.0)
    {
        try
        {
            parameters ??= new PaginationParameters();

            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query is required");
            }

            _logger.LogInformation("Searching for actors with query: {Query}, page: {Page}",
                query, parameters.PageNumber);

            var searchResult = await _tmdbService.SearchActorsAsync(query);
            if (searchResult?.Results == null || !searchResult.Results.Any())
            {
                return NotFound("No actors found matching the search criteria");
            }

            var filteredResults = minPopularity.HasValue
                ? searchResult.Results.Where(r => r.Popularity >= minPopularity.Value)
                : searchResult.Results;

            var results = filteredResults
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.ProfilePath,
                    r.Popularity,
                    IsTracked = _context.Actors.Any(a => a.TmdbId == r.Id)
                });

            var pagedResponse = new PagedResponse<dynamic>
            {
                Data = results,
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize,
                TotalRecords = searchResult.TotalResults,
                TotalPages = searchResult.TotalPages
            };

            return Ok(pagedResponse);
        }
        catch (TmdbApiException ex) when (ex.StatusCode == 429)
        {
            _logger.LogWarning(ex, "TMDB API rate limit exceeded");
            return StatusCode(429, "Search service is temporarily unavailable. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during actor search: {Error}", ex.Message);
            return StatusCode(500, "An error occurred while searching for actors");
        }
    }

    [HttpGet("filter-deaths")]
    public async Task<ActionResult<PagedResponse<dynamic>>> FilterDeaths(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? causeOfDeath = null,
        [FromQuery] string? placeOfDeath = null,
        [FromQuery] PaginationParameters parameters = null)
    {
        try
        {
            parameters ??= new PaginationParameters();
            _logger.LogInformation("Filtering actors by death criteria");

            var query = _context.Actors
                .Include(a => a.DeathRecord)
                .Where(a => a.DateOfDeath.HasValue)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                var fromDateOnly = DateOnly.FromDateTime(fromDate.Value);
                query = query.Where(a => a.DateOfDeath >= fromDateOnly);
            }

            if (toDate.HasValue)
            {
                var toDateOnly = DateOnly.FromDateTime(toDate.Value);
                query = query.Where(a => a.DateOfDeath <= toDateOnly);
            }

            if (!string.IsNullOrWhiteSpace(placeOfDeath))
            {
                query = query.Where(a => a.DeathRecord != null &&
                                       a.DeathRecord.PlaceOfDeath != null &&
                                       a.DeathRecord.PlaceOfDeath.Contains(placeOfDeath));
            }

            var totalRecords = await query.CountAsync();

            var results = await query
                .OrderByDescending(a => a.DateOfDeath)
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(a => new
                {
                    a.Id,
                    a.TmdbId,
                    a.Name,
                    a.ProfileImagePath,
                    DateOfBirth = a.DateOfBirth,
                    DateOfDeath = a.DateOfDeath,
                    DeathRecord = a.DeathRecord == null ? null : new
                    {
                        a.DeathRecord.CauseOfDeath,
                        a.DeathRecord.PlaceOfDeath,
                        a.DeathRecord.AdditionalDetails,
                        a.DeathRecord.SourceUrl,
                        a.DeathRecord.LastVerified
                    }
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<dynamic>
            {
                Data = results,
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)parameters.PageSize)
            };

            if (!results.Any())
            {
                return NotFound("No actors found matching the specified criteria");
            }

            return Ok(pagedResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering actors by death criteria");
            return StatusCode(500, "An error occurred while filtering actors");
        }
    }


    [HttpGet("test-tmdb")]
    public async Task<IActionResult> TestTmdbConnection()
    {
        try
        {
            await _tmdbService.TestConnectionAsync();
            return Ok("TMDB connection test completed - check logs for details");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TMDB connection test failed");
            return StatusCode(500, "TMDB connection test failed - check logs for details");
        }
    }

    private bool ActorExists(int id)
    {
        return _context.Actors.Any(e => e.Id == id);
    }
}