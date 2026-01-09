// src/lib/services/SearchService.test.ts
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

// FIX: Remove the 'type' keyword here
import { SearchService } from "$lib/services/SearchService";

describe('SearchService', () => {
    // TypeScript still knows the type of 'service' from the standard import
    let service: SearchService;

    beforeEach(() => {
        // Now 'SearchService' is available as a value to be instantiated
        service = new SearchService();

        vi.stubGlobal('fetch', vi.fn());
    });

    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it('should call searchDatasets with the correct query', async () => {
        const mockData = [{ title: 'Test Dataset', confidenceScore: 0.9 }];

        vi.mocked(fetch).mockResolvedValue({
            ok: true,
            json: () => Promise.resolve(mockData)
        } as Response);

        const results = await service.searchDatasets('climate');

        expect(fetch).toHaveBeenCalledWith(
            expect.stringContaining('/Search'),
            expect.objectContaining({
                method: 'POST',
                body: JSON.stringify({ query: 'climate' })
            })
        );
        expect(results).toEqual(mockData);
    });
});