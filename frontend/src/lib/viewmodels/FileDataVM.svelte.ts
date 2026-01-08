// lib/viewmodels/FileDataVM.svelte.ts
import type { DataFile } from '$lib/models/DataFile';

export class FileDataVM {
    // State is now typed as an array of DataFile objects
    files = $state<DataFile[]>([]);
    isLoading = $state(false);
    showModal = $state(false);
    error = $state<string | null>(null);

    async loadFiles(datasetId: string | undefined) {
        if (!datasetId) return;

        this.showModal = true;
        this.isLoading = true;
        this.error = null;
        this.files = [];

        try {
            const response = await fetch(`/api/Search/${datasetId}/files`);
            if (!response.ok) throw new Error("Failed to load files");

            // The JSON from your C# API should match the DataFile structure
            const data: DataFile[] = await response.json();
            this.files = data;
        } catch (err: any) {
            this.error = err.message;
            console.error("FileDataVM Error:", err);
        } finally {
            this.isLoading = false;
        }
    }

    close() {
        this.showModal = false;
        this.files = [];
    }
}