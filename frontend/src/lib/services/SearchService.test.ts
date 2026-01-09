// src/lib/services/SearchService.test.ts
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { SearchService } from './SearchService';

describe('SearchService', () => {
    let service: SearchService;

    beforeEach(() => {
        service = new SearchService();

        // FIX: Use vi.stubGlobal instead of global.fetch
        // This makes 'fetch' available globally as a Vitest mock function
        vi.stubGlobal('fetch', vi.fn());
    });

    afterEach(() => {
        // Best Practice: Clean up the global stub after each test
        vi.unstubAllGlobals();
    });

    it('should call searchDatasets with the correct query', async () => {
        const mockData = [{ title: 'Test Dataset', confidenceScore: 0.9 }];

        // FIX: Use vi.mocked() to get type-safe access to the mocked fetch
        vi.mocked(fetch).mockResolvedValue({
            ok: true,
            json: () => Promise.resolve(mockData)
        } as Response);

        const results = await service.searchDatasets('climate');

        // Assertion remains the same, but now targets the global fetch
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