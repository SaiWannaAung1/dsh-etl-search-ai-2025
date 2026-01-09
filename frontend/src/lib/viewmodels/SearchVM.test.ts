// src/lib/viewmodels/SearchVM.test.ts
import { describe, it, expect, vi } from 'vitest';
import { SearchVM } from './SearchVM.svelte';
import { searchService } from '$lib/services/SearchService';

// Mock the searchService module
vi.mock('$lib/services/SearchService', () => ({
    searchService: {
        searchDatasets: vi.fn()
    }
}));

describe('SearchVM', () => {
    it('should fetch and sort results by confidenceScore', async () => {
        const vm = new SearchVM();
        const mockResults = [
            { title: 'Low Score', confidenceScore: 0.5 },
            { title: 'High Score', confidenceScore: 0.9 }
        ];

        (searchService.searchDatasets as any).mockResolvedValue(mockResults);

        vm.query = 'test';
        const searchPromise = vm.search();

        // Test loading state (runes work in Vitest with jsdom)
        expect(vm.isSearching).toBe(true);

        await searchPromise;

        expect(vm.isSearching).toBe(false);
        expect(vm.results[0].title).toBe('High Score'); // Sorted first
        expect(vm.results[1].title).toBe('Low Score');
    });

    it('should handle errors gracefully', async () => {
        const vm = new SearchVM();
        (searchService.searchDatasets as any).mockRejectedValue(new Error('API Down'));

        vm.query = 'fail';
        await vm.search();

        expect(vm.results).toEqual([]);
        expect(vm.isSearching).toBe(false);
    });
});