using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeathflixAPI.Data;
using DeathflixAPI.Models;

namespace DeathflixAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActorsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ActorsController> _logger;

    // Constructor - inject the database context
    public ActorsController(AppDbContext context, ILogger<ActorsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/actors
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Actor>>> GetActors()
    {
        return await _context.Actors.ToListAsync();
    }

    // GET: api/actors/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Actor>> GetActor(int id)
    {
        var actor = await _context.Actors
            .Include(a => a.DeathRecord)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (actor == null)
        {
            return NotFound();
        }

        return actor;
    }

    // POST: api/actors
    [HttpPost]
    public async Task<ActionResult<Actor>> CreateActor(Actor actor)
    {
        _context.Actors.Add(actor);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetActor),
            new { id = actor.Id },
            actor);
    }

    // PUT: api/actors/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateActor(int id, Actor actor)
    {
        if (id != actor.Id)
        {
            return BadRequest();
        }

        _context.Entry(actor).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ActorExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // DELETE: api/actors/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteActor(int id)
    {
        var actor = await _context.Actors.FindAsync(id);
        if (actor == null)
        {
            return NotFound();
        }

        _context.Actors.Remove(actor);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ActorExists(int id)
    {
        return _context.Actors.Any(e => e.Id == id);
    }
}