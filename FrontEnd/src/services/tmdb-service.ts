interface TmdbActorResponse {
    id: number;
    name: string;
    profile_path: string | null;
    biography?: string;
    birthday?: string;
    place_of_birth?: string;
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
}