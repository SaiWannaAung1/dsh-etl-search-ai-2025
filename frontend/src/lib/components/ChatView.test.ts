import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
// Standard import (not import type) so we can use 'new'
import { ChatService } from "$lib/services/ChatService";

describe('ChatService', () => {
    let service: ChatService;

    beforeEach(() => {
        // Instantiate the service for each test
        service = new ChatService();

        // Mock the global fetch used by BaseService
        vi.stubGlobal('fetch', vi.fn());
    });

    afterEach(() => {
        // Clean up global stubs to avoid leaking into other tests
        vi.unstubAllGlobals();
    });

    it('should call ask with the correct prompt and history', async () => {
        // 1. Setup mock data matching your ChatResponse model
        const mockResponse = {
            answer: 'The carbon cycle is...',
            sources: [{ fileName: 'carbon_study.pdf', storagePath: '/files/1' }]
        };

        // 2. Mock the fetch implementation
        vi.mocked(fetch).mockResolvedValue({
            ok: true,
            json: () => Promise.resolve(mockResponse)
        } as Response);

        // 3. Define the request payload
        const chatRequest = {
            message: 'Explain carbon',
            history: []
        };

        // 4. Execute the service method
        const results = await service.ask(chatRequest);

        // 5. Verify the API call (Endpoint and Method)
        expect(fetch).toHaveBeenCalledWith(
            expect.stringContaining('/Chat/ask'),
            expect.objectContaining({
                method: 'POST',
                body: JSON.stringify(chatRequest)
            })
        );

        // 6. Verify the returned data matches our mock
        expect(results.answer).toBe('The carbon cycle is...');
        expect(results.sources).toHaveLength(1);
    });
});