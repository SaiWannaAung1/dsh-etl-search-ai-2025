// src/lib/services/ChatService.test.ts
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ChatService } from './ChatService';

describe('ChatService', () => {
    let service: ChatService;

    beforeEach(() => {
        service = new ChatService();
        vi.stubGlobal('fetch', vi.fn());
    });

    it('should send the prompt and history to the backend', async () => {
        const mockResponse = { answer: 'The sky is blue', sources: [] };
        vi.mocked(fetch).mockResolvedValue({
            ok: true,
            json: () => Promise.resolve(mockResponse)
        } as Response);

        const result = await service.ask({
            message: 'Hello',
            history: []
        });

        expect(fetch).toHaveBeenCalledWith(
            expect.stringContaining('/Chat/ask'),
            expect.objectContaining({ method: 'POST' })
        );
        expect(result.answer).toBe('The sky is blue');
    });
});