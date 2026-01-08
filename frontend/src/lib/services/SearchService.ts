import { BaseService } from './BaseService';
import type { Dataset } from '$lib/models/Dataset';

export class SearchService extends BaseService {
    async searchDatasets(query: string): Promise<Dataset[]> {
        // Just call the inherited 'post' method
        // Endpoint starts with / because it appends to BASE_URL
        return await this.post<Dataset[]>('/Search', { query });
    }
}

export const searchService = new SearchService();