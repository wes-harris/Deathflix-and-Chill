using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using DeathflixAPI.Controllers;
using DeathflixAPI.Data;
using DeathflixAPI.Models;
using DeathflixAPI.Services;

namespace DeathflixAPI.Tests.Controllers;

public class ActorsControllerTests
{
    private readonly Mock<ILogger<ActorsController>> _loggerMock;
    private readonly Mock<ITmdbService> _tmdbServiceMock;
    private readonly DbContextOptions<AppDbContext> _dbContextOptions;

    public ActorsControllerTests()
    {
        _loggerMock = new Mock<ILogger<ActorsController>>();
        _tmdbServiceMock = new Mock<ITmdbService>();
        _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;
    }

    private AppDbContext CreateContext()
    {
        var context = new AppDbContext(_dbContextOptions);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        return context;
    }

    // GET Tests
    [Fact]
    public async Task GetActors_ReturnsOkResult_WithPaginatedList()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);
        var parameters = new ActorParameters
        {
            Pagination = new PaginationParameters { PageNumber = 1, PageSize = 10 },
            Sorting = new SortingParameters { SortBy = "name", Direction = SortDirection.Ascending }
        };

        // Create test data
        var actors = new List<Actor>
        {
            new() { Id = 1, Name = "Actor 1", TmdbId = 1 },
            new() { Id = 2, Name = "Actor 2", TmdbId = 2 }
        };
        await context.Actors.AddRangeAsync(actors);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetActors(parameters);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResponse = Assert.IsType<PagedResponse<Actor>>(okResult.Value);
        Assert.Equal(2, pagedResponse.TotalRecords);
        Assert.Equal(2, pagedResponse.Data.Count());
    }

    [Fact]
    public async Task GetActor_ReturnsNotFound_WhenActorDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);

        // Act
        var result = await controller.GetActor(1);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetActor_ReturnsOkResult_WhenActorExists()
    {
        // Arrange
        using var context = CreateContext();
        var actor = new Actor { Id = 1, Name = "Test Actor", TmdbId = 1 };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);

        // Act
        var result = await controller.GetActor(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedActor = Assert.IsType<Actor>(okResult.Value);
        Assert.Equal(actor.Name, returnedActor.Name);
    }

    // POST Tests
    [Fact]
    public async Task CreateActor_ReturnsCreatedAtAction_WithNewActor()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);
        var newActor = new Actor { Name = "New Actor", TmdbId = 1 };

        // Act
        var result = await controller.CreateActor(newActor);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedActor = Assert.IsType<Actor>(createdAtActionResult.Value);
        Assert.Equal(newActor.Name, returnedActor.Name);
        Assert.Equal(1, await context.Actors.CountAsync());
    }

    [Fact]
    public async Task CreateActor_ReturnsBadRequest_WhenActorIsNull()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);

        // Act
        var result = await controller.CreateActor(null);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // PUT Tests
    [Fact]
    public async Task UpdateActor_ReturnsNoContent_WhenUpdateSuccessful()
    {
        // Arrange
        using var context = CreateContext();
        var actor = new Actor { Id = 1, Name = "Original Name", TmdbId = 1 };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);
        actor.Name = "Updated Name";

        // Act
        var result = await controller.UpdateActor(1, actor);

        // Assert
        Assert.IsType<NoContentResult>(result);
        var updatedActor = await context.Actors.FindAsync(1);
        Assert.Equal("Updated Name", updatedActor.Name);
    }

    // DELETE Tests
    [Fact]
    public async Task DeleteActor_ReturnsNoContent_WhenActorExists()
    {
        // Arrange
        using var context = CreateContext();
        var actor = new Actor { Id = 1, Name = "Test Actor", TmdbId = 1 };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);

        // Act
        var result = await controller.DeleteActor(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, await context.Actors.CountAsync());
    }

    // TMDB Integration Tests
    [Fact]
    public async Task GetActorDetails_ReturnsOkResult_WithCombinedData()
    {
        // Arrange
        using var context = CreateContext();
        var actor = new Actor { Id = 1, Name = "Test Actor", TmdbId = 1 };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var tmdbDetails = new TmdbActorDetails
        {
            Biography = "Test Bio",
            Birthday = "1990-01-01",
            PlaceOfBirth = "Test City"
        };
        _tmdbServiceMock.Setup(x => x.GetActorDetailsAsync(1))
            .ReturnsAsync(tmdbDetails);

        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);

        // Act
        var result = await controller.GetActorDetails(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        dynamic returnedData = okResult.Value;
        Assert.Equal("Test Actor", returnedData.Name);
        Assert.Equal("Test Bio", returnedData.Biography);
    }

    // Search Tests
    [Fact]
    public async Task SearchActors_ReturnsOkResult_WithPaginatedResults()
    {
        // Arrange
        using var context = CreateContext();
        var searchResult = new TmdbActorResponse
        {
            Page = 1,
            TotalPages = 2,
            TotalResults = 15,
            Results = new List<TmdbActorResult>
        {
            new() { Id = 1, Name = "Test Actor", Popularity = 5.5, ProfilePath = "/test.jpg" },
            new() { Id = 2, Name = "Another Actor", Popularity = 6.0, ProfilePath = "/test2.jpg" }
        }
        };

        _tmdbServiceMock.Setup(x => x.SearchActorsAsync("test"))
            .ReturnsAsync(searchResult);

        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);
        var parameters = new ActorParameters
        {
            Pagination = new PaginationParameters { PageNumber = 1, PageSize = 10 },
            Sorting = new SortingParameters { SortBy = "popularity", Direction = SortDirection.Descending }
        };

        // Act
        var result = await controller.SearchActors("test", parameters, 5.0);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResponse = Assert.IsType<PagedResponse<dynamic>>(okResult.Value);
        Assert.Equal(2, pagedResponse.Data.Count());
        Assert.Equal(15, pagedResponse.TotalRecords);
    }

    [Fact]
    public async Task SearchActors_ReturnsBadRequest_WhenQueryIsEmpty()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);
        var parameters = new ActorParameters();

        // Act
        var result = await controller.SearchActors("", parameters);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // Deceased Actors Tests
    [Fact]
    public async Task GetRecentlyDeceasedActors_ReturnsOkResult_WithPaginatedList()
    {
        // Arrange
        using var context = CreateContext();
        var actors = new List<Actor>
    {
        new()
        {
            Id = 1,
            Name = "Deceased Actor 1",
            DateOfDeath = DateOnly.FromDateTime(DateTime.Now.AddDays(-10)),
            DeathRecord = new DeathRecord { LastVerified = DateTime.Now }
        },
        new()
        {
            Id = 2,
            Name = "Deceased Actor 2",
            DateOfDeath = DateOnly.FromDateTime(DateTime.Now.AddDays(-5)),
            DeathRecord = new DeathRecord { LastVerified = DateTime.Now }
        }
    };
        await context.Actors.AddRangeAsync(actors);
        await context.SaveChangesAsync();

        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);
        var parameters = new ActorParameters
        {
            Pagination = new PaginationParameters { PageNumber = 1, PageSize = 10 },
            Sorting = new SortingParameters { SortBy = "deathdate", Direction = SortDirection.Descending }
        };

        // Act
        var result = await controller.GetRecentlyDeceasedActors(parameters, DateTime.Now.AddDays(-15), DateTime.Now);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResponse = Assert.IsType<PagedResponse<dynamic>>(okResult.Value);
        Assert.Equal(2, pagedResponse.TotalRecords);
    }

    [Fact]
    public async Task GetRecentlyDeceasedActors_FiltersCorrectly_ByDateRange()
    {
        // Arrange
        using var context = CreateContext();
        var actors = new List<Actor>
    {
        new()
        {
            Id = 1,
            Name = "Old Death",
            DateOfDeath = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)),
            DeathRecord = new DeathRecord { LastVerified = DateTime.Now }
        },
        new()
        {
            Id = 2,
            Name = "Recent Death",
            DateOfDeath = DateOnly.FromDateTime(DateTime.Now.AddDays(-5)),
            DeathRecord = new DeathRecord { LastVerified = DateTime.Now }
        }
    };
        await context.Actors.AddRangeAsync(actors);
        await context.SaveChangesAsync();

        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);
        var parameters = new ActorParameters();

        // Act
        var result = await controller.GetRecentlyDeceasedActors(
            parameters,
            DateTime.Now.AddDays(-10),  // Since 10 days ago
            DateTime.Now);              // Until now

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResponse = Assert.IsType<PagedResponse<dynamic>>(okResult.Value);
        Assert.Equal(1, pagedResponse.TotalRecords);  // Should only get the recent death
    }

    // Complex Pagination and Sorting Tests
    [Fact]
    public async Task GetActors_SortsCorrectly_ByMultipleCriteria()
    {
        // Arrange
        using var context = CreateContext();
        var actors = new List<Actor>
    {
        new() { Id = 1, Name = "Actor A", Popularity = 5.0 },
        new() { Id = 2, Name = "Actor B", Popularity = 8.0 },
        new() { Id = 3, Name = "Actor C", Popularity = 5.0 }
    };
        await context.Actors.AddRangeAsync(actors);
        await context.SaveChangesAsync();

        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);
        var parameters = new ActorParameters
        {
            Pagination = new PaginationParameters { PageNumber = 1, PageSize = 10 },
            Sorting = new SortingParameters { SortBy = "popularity", Direction = SortDirection.Descending }
        };

        // Act
        var result = await controller.GetActors(parameters);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResponse = Assert.IsType<PagedResponse<Actor>>(okResult.Value);
        var resultList = pagedResponse.Data.ToList();
        Assert.Equal("Actor B", resultList[0].Name);  // Highest popularity should be first
    }

    [Fact]
    public async Task GetActors_HandlesPagination_WithPartialPage()
    {
        // Arrange
        using var context = CreateContext();
        var actors = Enumerable.Range(1, 15)  // Create 15 actors
            .Select(i => new Actor { Id = i, Name = $"Actor {i}", TmdbId = i })
            .ToList();
        await context.Actors.AddRangeAsync(actors);
        await context.SaveChangesAsync();

        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);
        var parameters = new ActorParameters
        {
            Pagination = new PaginationParameters { PageNumber = 2, PageSize = 10 },  // Should get remaining 5 actors
            Sorting = new SortingParameters { SortBy = "name", Direction = SortDirection.Ascending }
        };

        // Act
        var result = await controller.GetActors(parameters);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var pagedResponse = Assert.IsType<PagedResponse<Actor>>(okResult.Value);
        Assert.Equal(15, pagedResponse.TotalRecords);
        Assert.Equal(5, pagedResponse.Data.Count());  // Should get 5 items on the second page
        Assert.Equal(2, pagedResponse.TotalPages);
    }

    // Error Handling Tests
    [Fact]
    public async Task SearchActors_HandlesRateLimitException_Returns429()
    {
        // Arrange
        using var context = CreateContext();
        _tmdbServiceMock.Setup(x => x.SearchActorsAsync(It.IsAny<string>()))
            .ThrowsAsync(new TmdbApiException("Rate limit exceeded", 429));

        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);
        var parameters = new ActorParameters();

        // Act
        var result = await controller.SearchActors("test", parameters);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(429, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetActorDetails_HandlesTmdbServiceError_ReturnsBasicInfo()
    {
        // Arrange
        using var context = CreateContext();
        var actor = new Actor { Id = 1, Name = "Test Actor", TmdbId = 1 };
        await context.Actors.AddAsync(actor);
        await context.SaveChangesAsync();

        _tmdbServiceMock.Setup(x => x.GetActorDetailsAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("TMDB Service Error"));

        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);

        // Act
        var result = await controller.GetActorDetails(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(actor, okResult.Value);
    }

    [Fact]
    public async Task UpdateActor_HandlesConcurrencyError_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateContext();
        var actor = new Actor { Id = 1, Name = "Original Name", TmdbId = 1 };
        await context.Actors.AddAsync(actor);
        await context.SaveChangesAsync();

        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);

        // Simulate concurrent deletion
        context.Actors.Remove(actor);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.UpdateActor(1, actor);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetActors_HandlesDbError_Returns500()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);

        // Force database error by disposing context
        await context.DisposeAsync();

        // Act
        var result = await controller.GetActors(new ActorParameters());

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task CreateActor_HandlesDbUpdateException_Returns500()
    {
        // Arrange
        using var context = CreateContext();
        var actor = new Actor { Id = 1, Name = "Test Actor", TmdbId = 1 };

        // Create a mock DbContext that throws on SaveChanges
        var mockContext = new Mock<AppDbContext>(_dbContextOptions);
        mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("DB Error", new Exception()));
        mockContext.Setup(x => x.Actors).Returns(context.Actors);

        var controller = new ActorsController(mockContext.Object, _loggerMock.Object, _tmdbServiceMock.Object);

        // Act
        var result = await controller.CreateActor(actor);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetRecentlyDeceasedActors_HandlesInvalidDateRange_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);
        var parameters = new ActorParameters();
        var futureDate = DateTime.Now.AddDays(1);
        var pastDate = DateTime.Now.AddDays(-1);

        // Act
        var result = await controller.GetRecentlyDeceasedActors(parameters, futureDate, pastDate);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task DeleteActor_HandlesDbUpdateException_Returns500()
    {
        // Arrange
        using var context = CreateContext();
        var actor = new Actor { Id = 1, Name = "Test Actor", TmdbId = 1 };
        await context.Actors.AddAsync(actor);
        await context.SaveChangesAsync();

        // Create a mock DbContext that throws on SaveChanges
        var mockContext = new Mock<AppDbContext>(_dbContextOptions);
        mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("DB Error", new Exception()));
        mockContext.Setup(x => x.Actors).Returns(context.Actors);

        var controller = new ActorsController(mockContext.Object, _loggerMock.Object, _tmdbServiceMock.Object);

        // Act
        var result = await controller.DeleteActor(1);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // Validation Error Tests
    [Theory]
    [InlineData(0)]    // Invalid page number
    [InlineData(-1)]   // Negative page number
    public async Task GetActors_WithInvalidPageNumber_ReturnsBadRequest(int pageNumber)
    {
        // Arrange
        using var context = CreateContext();
        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);
        var parameters = new ActorParameters
        {
            Pagination = new PaginationParameters { PageNumber = pageNumber, PageSize = 10 }
        };

        // Act
        var result = await controller.GetActors(parameters);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("page number", badRequestResult.Value.ToString().ToLower());
    }

    [Fact]
    public async Task CreateActor_WithInvalidModel_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);
        var invalidActor = new Actor
        {
            Name = "",  // Name should not be empty
            TmdbId = -1 // Invalid TMDB ID
        };

        // Add model validation error
        controller.ModelState.AddModelError("Name", "Name is required");
        controller.ModelState.AddModelError("TmdbId", "TMDB ID must be positive");

        // Act
        var result = await controller.CreateActor(invalidActor);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateActor_WithMismatchedIds_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);
        var actor = new Actor { Id = 2, Name = "Test Actor", TmdbId = 1 };

        // Act
        var result = await controller.UpdateActor(1, actor); // ID 1 doesn't match actor.Id 2

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData("")]           // Empty string
    [InlineData("   ")]        // Whitespace
    [InlineData(null)]         // Null
    public async Task SearchActors_WithInvalidQuery_ReturnsBadRequest(string query)
    {
        // Arrange
        using var context = CreateContext();
        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);
        var parameters = new ActorParameters();

        // Act
        var result = await controller.SearchActors(query, parameters);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetRecentlyDeceasedActors_WithInvalidDateRange_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateContext();
        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);
        var parameters = new ActorParameters();
        var endDate = DateTime.Now.AddDays(-10);
        var startDate = endDate.AddDays(5); // Start date after end date

        // Act
        var result = await controller.GetRecentlyDeceasedActors(parameters, startDate, endDate);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("date range", badRequestResult.Value.ToString().ToLower());
    }

    [Theory]
    [InlineData(-1)]      // Negative page size
    [InlineData(0)]       // Zero page size
    [InlineData(1001)]    // Exceeds maximum page size
    public async Task GetActors_WithInvalidPageSize_ReturnsBadRequest(int pageSize)
    {
        // Arrange
        using var context = CreateContext();
        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);
        var parameters = new ActorParameters
        {
            Pagination = new PaginationParameters { PageNumber = 1, PageSize = pageSize }
        };

        // Act
        var result = await controller.GetActors(parameters);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("page size", badRequestResult.Value.ToString().ToLower());
    }

    [Fact]
    public async Task CreateActor_WithDuplicateTmdbId_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateContext();
        await context.Actors.AddAsync(new Actor { Name = "Existing Actor", TmdbId = 1 });
        await context.SaveChangesAsync();

        var controller = new ActorsController(context, _loggerMock.Object, _tmdbServiceMock.Object);
        var duplicateActor = new Actor { Name = "New Actor", TmdbId = 1 }; // Same TmdbId

        // Act
        var result = await controller.CreateActor(duplicateActor);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}