import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { TmdbService } from '../tmdb-service';

declare const global: any;
const fetchMock = vi.fn();
global.fetch = fetchMock;

describe('TmdbService', () => {
    let service: TmdbService;
    
    beforeEach(() => {
        service = new TmdbService('fake-api-key');
        fetchMock.mockClear();
        vi.useFakeTimers();
    });

    afterEach(() => {
        vi.restoreAllMocks();
        vi.useRealTimers();
    });

    describe('rate limiting', () => {
        it('should respect rate limits', async () => {
            vi.useFakeTimers(); // Use fake timers
            const startTime = Date.now();
            
            fetchMock.mockResolvedValue({
                ok: true,
                json: async () => ({ id: 1, name: "Test Actor" })
            });
        
            // Start all requests concurrently
            const requests = [
                service.getActorDetails(1),
                service.getActorDetails(2),
                service.getActorDetails(3),
                service.getActorDetails(4),
                service.getActorDetails(5)
            ];
        
            // Advance time as needed
            vi.advanceTimersByTime(1100); // Move time forward
            
            await Promise.all(requests);
        
            const endTime = Date.now();
            const timeTaken = endTime - startTime;
            expect(timeTaken).toBeGreaterThan(1000);
            
            vi.useRealTimers(); // Restore real timers
        });

        it('should handle concurrent requests without exceeding rate limit', async () => {
            vi.useFakeTimers();
            let lastCallTime = 0;
            
            fetchMock.mockImplementation(async () => {
                const currentTime = Number(vi.getTimerCount());
                if (lastCallTime > 0) {
                    const diff = currentTime - lastCallTime;
                    expect(diff).toBeGreaterThanOrEqual(250);
                }
                lastCallTime = currentTime;
                
                return {
                    ok: true,
                    json: async () => ({ id: 1, name: "Test Actor" })
                };
            });
        
            const requests = Array(5).fill(0).map((_, i) => 
                service.getActorDetails(i + 1)
            );
        
            // Process requests and advance time
            await Promise.all([
                Promise.all(requests),
                vi.advanceTimersByTimeAsync(2000)
            ]);
        
            vi.useRealTimers();
        });
    });

    describe('getActorDetails', () => {
        it('should fetch actor details successfully', async () => {
            const mockResponse = {
                id: 1,
                name: "Test Actor",
                profile_path: "/test.jpg",
                biography: "Test bio",
                birthday: "1990-01-01",
                place_of_birth: "Test City"
            };

            fetchMock.mockResolvedValueOnce({
                ok: true,
                json: async () => mockResponse
            });

            const result = await service.getActorDetails(1);

            expect(result).toBeDefined();
            expect(result.name).toBe("Test Actor");
            expect(result.profile_path).toContain('https://image.tmdb.org/t/p/original/test.jpg');
            expect(fetchMock).toHaveBeenCalledWith(
                expect.stringContaining('/person/1')
            );
        });

        it('should handle API errors gracefully', async () => {
            fetchMock.mockResolvedValueOnce({
                ok: false,
                status: 404
            });

            await expect(service.getActorDetails(1))
                .rejects
                .toThrow('Failed to fetch actor details');
        });

        it('should handle invalid actor ID', async () => {
            await expect(service.getActorDetails(-1))
                .rejects
                .toThrow('Invalid actor ID');
        });
    });

    describe('searchActors', () => {
        it('should search actors successfully', async () => {
            const mockResponse = {
                page: 1,
                results: [
                    {
                        id: 1,
                        name: "Test Actor",
                        profile_path: "/test.jpg",
                        popularity: 10.5
                    }
                ],
                total_pages: 1,
                total_results: 1
            };

            fetchMock.mockResolvedValueOnce({
                ok: true,
                json: async () => mockResponse
            });

            const result = await service.searchActors("Test Actor");

            expect(result.results).toBeDefined();
            expect(result.results[0].name).toBe("Test Actor");
            expect(result.results[0].profile_path).toContain('https://image.tmdb.org/t/p/original/test.jpg');
            expect(fetchMock).toHaveBeenCalledWith(
                expect.stringContaining('/search/person')
            );
        });

        it('should handle empty search query', async () => {
            await expect(service.searchActors(""))
                .rejects
                .toThrow('Search query cannot be empty');
        });

        it('should handle API errors in search', async () => {
            fetchMock.mockResolvedValueOnce({
                ok: false,
                status: 404
            });

            await expect(service.searchActors("Test"))
                .rejects
                .toThrow('Failed to search actors');
        });
    });

    describe('getActorMovieCredits', () => {
        it('should fetch movie credits successfully', async () => {
            const mockResponse = {
                cast: [{
                    id: 1,
                    title: "Test Movie",
                    character: "Test Character",
                    release_date: "2024-01-01",
                    poster_path: "/poster.jpg"
                }],
                crew: [{
                    id: 2,
                    title: "Test Movie 2",
                    job: "Director",
                    department: "Directing",
                    release_date: "2024-02-01",
                    poster_path: "/poster2.jpg"
                }]
            };

            fetchMock.mockResolvedValueOnce({
                ok: true,
                json: async () => mockResponse
            });

            const result = await service.getActorMovieCredits(1);

            expect(result.cast).toBeDefined();
            expect(result.crew).toBeDefined();
            expect(result.cast[0].title).toBe("Test Movie");
            expect(result.crew[0].job).toBe("Director");
            expect(result.cast[0].poster_path).toContain('https://image.tmdb.org/t/p/original/poster.jpg');
            expect(fetchMock).toHaveBeenCalledWith(
                expect.stringContaining('/person/1/movie_credits')
            );
        });

        it('should handle invalid actor ID for credits', async () => {
            await expect(service.getActorMovieCredits(-1))
                .rejects
                .toThrow('Invalid actor ID');
        });

        it('should handle API errors in movie credits', async () => {
            fetchMock.mockResolvedValueOnce({
                ok: false,
                status: 404
            });

            await expect(service.getActorMovieCredits(1))
                .rejects
                .toThrow('Failed to fetch actor movie credits');
        });

        it('should handle missing poster paths', async () => {
            const mockResponse = {
                cast: [{
                    id: 1,
                    title: "Test Movie",
                    character: "Test Character",
                    release_date: "2024-01-01",
                    poster_path: null
                }],
                crew: []
            };

            fetchMock.mockResolvedValueOnce({
                ok: true,
                json: async () => mockResponse
            });

            const result = await service.getActorMovieCredits(1);
            expect(result.cast[0].poster_path).toBeNull();
        });
    });
});