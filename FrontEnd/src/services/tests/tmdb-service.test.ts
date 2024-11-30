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
    });

    afterEach(() => {
        vi.restoreAllMocks();
    });

    describe('getActorDetails', () => {
        it('should fetch actor details successfully', async () => {
            const mockResponse = {
                id: 1,
                name: "Test Actor",
                profile_path: "/test.jpg",
                biography: "Test bio"
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
            global.fetch = vi.fn(() => 
                Promise.resolve({
                    ok: false,
                    status: 404
                })
            );

            await expect(service.getActorDetails(1))
                .rejects
                .toThrow('Failed to fetch actor details');
        });

        it('should handle network errors', async () => {
            global.fetch = vi.fn(() => 
                Promise.reject(new Error('Network error'))
            );

            await expect(service.getActorDetails(1))
                .rejects
                .toThrow('Network error');
        });

        it('should handle invalid actor ID', async () => {
            await expect(service.getActorDetails(-1))
                .rejects
                .toThrow('Invalid actor ID');
        });

        it('should construct full image URL for profile path', async () => {
            const mockResponse = {
                id: 1,
                name: "Test Actor",
                profile_path: "/test.jpg"
            };

            global.fetch = vi.fn(() => 
                Promise.resolve({
                    ok: true,
                    json: () => Promise.resolve(mockResponse)
                })
            );

            const result = await service.getActorDetails(1);
            expect(result.profile_path).toContain('https://image.tmdb.org/t/p/original/test.jpg');
        });

        it('should handle missing profile path', async () => {
            const mockResponse = {
                id: 1,
                name: "Test Actor",
                profile_path: null
            };

            global.fetch = vi.fn(() => 
                Promise.resolve({
                    ok: true,
                    json: () => Promise.resolve(mockResponse)
                })
            );

            const result = await service.getActorDetails(1);
            expect(result.profile_path).toBeNull();
        });
    });
});