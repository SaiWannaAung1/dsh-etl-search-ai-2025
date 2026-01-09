import { searchService } from '$lib/services/SearchService';
import type { Dataset } from '$lib/models/Dataset';

export class SearchVM {
    query = $state('');
    results = $state<Dataset[]>([]);
    isSearching = $state(false);

    async search() {
        if (!this.query.trim()) return;
        this.isSearching = true;

        try {
            const data = await searchService.searchDatasets(this.query);
            // Sort by confidenceScore descending
            this.results = data.sort((a, b) => b.confidenceScore - a.confidenceScore);
        } catch (error) {
            console.error(error);
            this.results = [];
        } finally {
            this.isSearching = false;
        }
    }
}