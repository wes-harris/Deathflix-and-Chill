using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeathflixAPI.Data;
using DeathflixAPI.Models;
using DeathflixAPI.Services;
using DeathflixAPI.Exceptions;


namespace DeathflixAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
/// <summary>
/// Controller for managing actor information, death records, and filmography details
/// </summary>
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

    /// <summary>
    /// Retrieves a paginated list of all actors
    /// </summary>
    /// <param name="parameters">Pagination and sorting parameters for the actor list</param>
    /// <returns>A paginated list of actors with sorting options</returns>
    /// <response code="200">Returns the paginated list of actors</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<Actor>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResponse<Actor>>> GetActors([FromQuery] ActorParameters parameters)
    {
        try
        {
            _logger.LogInformation("Retrieving actors with pagination and sorting");

            var query = _context.Actors.AsQueryable();

            // Apply sorting
            query = parameters.Sorting.SortBy.ToLower() switch
            {
                "name" => parameters.Sorting.Direction == SortDirection.Ascending
                    ? query.OrderBy(a => a.Name)
                    : query.OrderByDescending(a => a.Name),

                "birthdate" => parameters.Sorting.Direction == SortDirection.Ascending
                    ? query.OrderBy(a => a.DateOfBirth)
                    : query.OrderByDescending(a => a.DateOfBirth),

                "deathdate" => parameters.Sorting.Direction == SortDirection.Ascending
                    ? query.OrderBy(a => a.DateOfDeath)
                    : query.OrderByDescending(a => a.DateOfDeath),

                "popularity" => parameters.Sorting.Direction == SortDirection.Ascending
                    ? query.OrderBy(a => a.Popularity)
                    : query.OrderByDescending(a => a.Popularity),

                _ => query.OrderBy(a => a.Name) // default sorting
            };

            var totalRecords = await query.CountAsync();
            var actors = await query
                .Skip((parameters.Pagination.PageNumber - 1) * parameters.Pagination.PageSize)
                .Take(parameters.Pagination.PageSize)
                .ToListAsync();

            var pagedResponse = new PagedResponse<Actor>
            {
                Data = actors,
                PageNumber = parameters.Pagination.PageNumber,
                PageSize = parameters.Pagination.PageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)parameters.Pagination.PageSize)
            };

            return Ok(pagedResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving actors");
            return StatusCode(500, "An error occurred while retrieving actors");
        }
    }

    /// <summary>
    /// Retrieves a specific actor by their ID
    /// </summary>
    /// <param name="id">The unique identifier of the actor</param>
    /// <returns>Detailed information about the specified actor</returns>
    /// <response code="200">Returns the requested actor</response>
    /// <response code="404">If the actor was not found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Actor), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Creates a new actor record
    /// </summary>
    /// <param name="actor">The actor information to create</param>
    /// <returns>The newly created actor record</returns>
    /// <response code="201">Returns the newly created actor</response>
    /// <response code="400">If the actor data is invalid</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(Actor), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Updates an existing actor record
    /// </summary>
    /// <param name="id">The ID of the actor to update</param>
    /// <param name="actor">The updated actor information</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">If the actor was successfully updated</response>
    /// <response code="400">If the ID doesn't match the actor's ID</response>
    /// <response code="404">If the actor was not found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Deletes an actor record
    /// </summary>
    /// <param name="id">The ID of the actor to delete</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">If the actor was successfully deleted</response>
    /// <response code="404">If the actor was not found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Retrieves detailed information about an actor including TMDB data
    /// </summary>
    /// <param name="id">The ID of the actor</param>
    /// <returns>Combined actor details from local database and TMDB</returns>
    /// <response code="200">Returns the detailed actor information</response>
    /// <response code="404">If the actor was not found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("{id}/details")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Retrieves an actor's movie credits from TMDB
    /// </summary>
    /// <param name="id">The ID of the actor</param>
    /// <returns>List of movies the actor has appeared in</returns>
    /// <response code="200">Returns the actor's movie credits</response>
    /// <response code="404">If the actor was not found</response>
    /// <response code="400">If there was an error retrieving from TMDB</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("{id}/credits")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Retrieves a paginated list of recently deceased actors
    /// </summary>
    /// <param name="parameters">Pagination and sorting parameters</param>
    /// <param name="since">Optional start date to filter deaths</param>
    /// <param name="until">Optional end date to filter deaths</param>
    /// <returns>A paginated list of deceased actors</returns>
    /// <response code="200">Returns the list of deceased actors</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("deceased")]
    [ProducesResponseType(typeof(PagedResponse<dynamic>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResponse<dynamic>>> GetRecentlyDeceasedActors(
        [FromQuery] ActorParameters parameters,
        [FromQuery] DateTime? since = null,
        [FromQuery] DateTime? until = null)
    {
        try
        {
            _logger.LogInformation("Retrieving recently deceased actors");

            var query = _context.Actors
                .Include(a => a.DeathRecord)
                .Where(a => a.DateOfDeath.HasValue)
                .AsQueryable();

            // Apply date filters
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

            // Apply sorting
            query = parameters.Sorting.SortBy.ToLower() switch
            {
                "name" => parameters.Sorting.Direction == SortDirection.Ascending
                    ? query.OrderBy(a => a.Name)
                    : query.OrderByDescending(a => a.Name),

                "birthdate" => parameters.Sorting.Direction == SortDirection.Ascending
                    ? query.OrderBy(a => a.DateOfBirth)
                    : query.OrderByDescending(a => a.DateOfBirth),

                "deathdate" => parameters.Sorting.Direction == SortDirection.Ascending
                    ? query.OrderBy(a => a.DateOfDeath)
                    : query.OrderByDescending(a => a.DateOfDeath),

                "popularity" => parameters.Sorting.Direction == SortDirection.Ascending
                    ? query.OrderBy(a => a.Popularity)
                    : query.OrderByDescending(a => a.Popularity),

                _ => query.OrderByDescending(a => a.DateOfDeath) // default sorting for deceased
            };

            var totalRecords = await query.CountAsync();

            var deceasedActors = await query
                .Skip((parameters.Pagination.PageNumber - 1) * parameters.Pagination.PageSize)
                .Take(parameters.Pagination.PageSize)
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
                        AdditionalDetails = a.DeathRecord.AdditionalDetails,
                        SourceUrl = a.DeathRecord.SourceUrl,
                        LastVerified = a.DeathRecord.LastVerified
                    }
                })
                .ToListAsync();

            var pagedResponse = new PagedResponse<dynamic>
            {
                Data = deceasedActors,
                PageNumber = parameters.Pagination.PageNumber,
                PageSize = parameters.Pagination.PageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)parameters.Pagination.PageSize)
            };

            return Ok(pagedResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recently deceased actors");
            return StatusCode(500, "An error occurred while retrieving deceased actors");
        }
    }

    /// <summary>
    /// Searches for actors using the TMDB API
    /// </summary>
    /// <param name="query">The search query string</param>
    /// <param name="parameters">Pagination and sorting parameters</param>
    /// <param name="minPopularity">Minimum popularity score filter</param>
    /// <returns>A paginated list of actors matching the search criteria</returns>
    /// <response code="200">Returns the search results</response>
    /// <response code="400">If the search query is empty</response>
    /// <response code="404">If no actors were found</response>
    /// <response code="429">If TMDB rate limit is exceeded</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResponse<dynamic>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResponse<dynamic>>> SearchActors(
        [FromQuery] string query,
        [FromQuery] ActorParameters parameters,
        [FromQuery] double? minPopularity = 5.0)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query is required");
            }

            _logger.LogInformation("Searching for actors with query: {Query}, page: {Page}",
                query, parameters.Pagination.PageNumber);

            var searchResult = await _tmdbService.SearchActorsAsync(query);
            if (searchResult?.Results == null || !searchResult.Results.Any())
            {
                return NotFound("No actors found matching the search criteria");
            }

            // Filter by popularity if specified
            var filteredResults = minPopularity.HasValue
                ? searchResult.Results.Where(r => r.Popularity >= minPopularity.Value)
                : searchResult.Results;

            // Apply sorting
            var sortedResults = parameters.Sorting.SortBy.ToLower() switch
            {
                "name" => parameters.Sorting.Direction == SortDirection.Ascending
                    ? filteredResults.OrderBy(r => r.Name)
                    : filteredResults.OrderByDescending(r => r.Name),

                "popularity" => parameters.Sorting.Direction == SortDirection.Ascending
                    ? filteredResults.OrderBy(r => r.Popularity)
                    : filteredResults.OrderByDescending(r => r.Popularity),

                _ => filteredResults.OrderByDescending(r => r.Popularity) // default sorting
            };

            // Apply pagination
            var pagedResults = sortedResults
                .Skip((parameters.Pagination.PageNumber - 1) * parameters.Pagination.PageSize)
                .Take(parameters.Pagination.PageSize)
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
                Data = pagedResults,
                PageNumber = parameters.Pagination.PageNumber,
                PageSize = parameters.Pagination.PageSize,
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

    /// <summary>
    /// Tests the connection to the TMDB API
    /// </summary>
    /// <returns>A success message if the connection test passed</returns>
    /// <response code="200">If the connection test was successful</response>
    /// <response code="500">If the connection test failed</response>
    [HttpGet("test-tmdb")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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