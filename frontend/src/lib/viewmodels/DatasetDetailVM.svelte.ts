import type { DatasetDetail } from '$lib/models/DatasetDetail';
import {datasetDetailService} from "$lib/services/DatasetService";

export class DatasetDetailVM {
    // --- Reactive States ---
    data = $state<DatasetDetail | null>(null);
    isLoading = $state(false);
    error = $state<string | null>(null);

    /**
     * Loads the full dataset metadata by ID
     */
    async load(id: string) {
        if (!id) {
            this.error = "Invalid Dataset ID";
            return;
        }

        this.isLoading = true;
        this.error = null;

        try {
            this.data = await datasetDetailService.getDatasetById(id);
        } catch (e: any) {
            this.error = e.message || "An unexpected error occurred while loading data.";
            console.error("DatasetDetailVM Error:", e);
        } finally {
            this.isLoading = false;
        }
    }

    // --- Computed Helpers (Logic for the View) ---

    /**
     * Splits the comma-separated keywords into a clean array for UI tags
     */
    get keywordList(): string[] {
        if (!this.data?.keywords) return [];
        return this.data.keywords.split(',')
            .map(k => k.trim())
            .filter(k => k.length > 0);
    }

    /**
     * Splits the combined authors string into an array of objects
     */
    get authorList() {
        if (!this.data?.authors) return [];
        return this.data.authors.split(' / ').map(entry => {
            const [name, org] = entry.split(' from ');
            return { name, organization: org || 'Unknown' };
        });
    }

    /**
     * Formats the publication date for display
     */
    get formattedDate(): string {
        if (!this.data?.publishedDate) return 'N/A';
        return new Date(this.data.publishedDate).toLocaleDateString('en-GB', {
            day: 'numeric',
            month: 'long',
            year: 'numeric'
        });
    }
}