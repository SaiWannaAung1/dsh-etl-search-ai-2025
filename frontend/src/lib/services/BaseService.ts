import { API_CONFIG } from '$lib/config';

export class BaseService {
    protected baseUrl = API_CONFIG.BASE_URL;

    // A helper for POST requests used by all child services
    protected async post<T>(endpoint: string, body: any): Promise<T> {
        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });

        if (!response.ok) {
            throw new Error(`API Error: ${response.statusText}`);
        }

        return await response.json();
    }

    // --- NEW: Helper for GET requests (Dataset Details, etc.) ---
    protected async get<T>(endpoint: string): Promise<T> {
        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            method: 'GET',
            headers: { 'Content-Type': 'application/json' }
        });

        if (!response.ok) {
            // Check for specific status codes like 404
            if (response.status === 404) {
                throw new Error('The requested resource was not found.');
            }
            throw new Error(`API Error: ${response.statusText}`);
        }

        return await response.json();
    }
}