interface TmdbActorResponse {
    id: number;
    name: string;
    profile_path: string | null;
    biography?: string;
    birthday?: string;
    place_of_birth?: string;
}

interface TmdbSearchResult {
    page: number;
    results: {
        id: number;
        name: string;
        profile_path: string | null;
        popularity: number;
    }[];
    total_pages: number;
    total_results: number;
}

interface TmdbMovieCredits {
    cast: {
        id: number;
        title: string;
        character: string;
        release_date: string;
        poster_path: string | null;
    }[];
    crew: {
        id: number;
        title: string;
        job: string;
        department: string;
        release_date: string;
        poster_path: string | null;
    }[];
}

export class TmdbService {
    private readonly baseUrl = 'https://api.themoviedb.org/3';
    private readonly imageBaseUrl = 'https://image.tmdb.org/t/p/original';

    constructor(private apiKey: string) {}

    async getActorDetails(id: number): Promise<TmdbActorResponse> {
        if (id <= 0) {
            throw new Error('Invalid actor ID');
        }

        try {
            const response = await fetch(
                `${this.baseUrl}/person/${id}?api_key=${this.apiKey}&language=en-US`
            );

            if (!response.ok) {
                throw new Error('Failed to fetch actor details');
            }

            const data = await response.json();
            return {
                id: data.id,
                name: data.name,
                biography: data.biography,
                birthday: data.birthday,
                place_of_birth: data.place_of_birth,
                profile_path: data.profile_path ? 
                    `${this.imageBaseUrl}${data.profile_path}` : null
            };
        } catch (error) {
            if (error instanceof Error) {
                throw error;
            }
            throw new Error('Failed to fetch actor details');
        }
    }

    async searchActors(query: string): Promise<TmdbSearchResult> {
        if (!query.trim()) {
            throw new Error('Search query cannot be empty');
        }

        try {
            const response = await fetch(
                `${this.baseUrl}/search/person?api_key=${this.apiKey}&query=${encodeURIComponent(query)}&include_adult=false&language=en-US`
            );

            if (!response.ok) {
                throw new Error('Failed to search actors');
            }

            const data = await response.json();
            return {
                page: data.page,
                results: data.results.map((actor: any) => ({
                    id: actor.id,
                    name: actor.name,
                    profile_path: actor.profile_path ? 
                        `${this.imageBaseUrl}${actor.profile_path}` : null,
                    popularity: actor.popularity
                })),
                total_pages: data.total_pages,
                total_results: data.total_results
            };
        } catch (error) {
            if (error instanceof Error) {
                throw error;
            }
            throw new Error('Failed to search actors');
        }
    }

    async getActorMovieCredits(id: number): Promise<TmdbMovieCredits> {
        if (id <= 0) {
            throw new Error('Invalid actor ID');
        }

        try {
            const response = await fetch(
                `${this.baseUrl}/person/${id}/movie_credits?api_key=${this.apiKey}&language=en-US`
            );

            if (!response.ok) {
                throw new Error('Failed to fetch actor movie credits');
            }

            const data = await response.json();
            return {
                cast: data.cast.map((movie: any) => ({
                    id: movie.id,
                    title: movie.title,
                    character: movie.character,
                    release_date: movie.release_date,
                    poster_path: movie.poster_path ? 
                        `${this.imageBaseUrl}${movie.poster_path}` : null
                })),
                crew: data.crew.map((movie: any) => ({
                    id: movie.id,
                    title: movie.title,
                    job: movie.job,
                    department: movie.department,
                    release_date: movie.release_date,
                    poster_path: movie.poster_path ? 
                        `${this.imageBaseUrl}${movie.poster_path}` : null
                }))
            };
        } catch (error) {
            if (error instanceof Error) {
                throw error;
            }
            throw new Error('Failed to fetch actor movie credits');
        }
    }
}