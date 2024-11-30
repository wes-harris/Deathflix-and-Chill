using DeathflixAPI.Models.Tmdb;

namespace DeathflixAPI.Services;

public interface ITmdbService
{
    Task<TmdbActorResponse?> SearchActorsAsync(string query);
    Task<TmdbActorDetails?> GetActorDetailsAsync(int tmdbId);
    Task<TmdbMovieCredits?> GetActorMovieCreditsAsync(int tmdbId);
    Task<List<TmdbActorDetails>> GetRecentlyDeceasedActorsAsync();
    Task TestConnectionAsync();
    Task<TmdbActorResponse?> GetAllActorsAsync(int page = 1);
}