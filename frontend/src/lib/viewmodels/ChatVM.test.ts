// src/lib/viewmodels/ChatVM.test.ts
import { describe, it, expect, vi } from 'vitest';
import { ChatVM } from './ChatVM.svelte';
import { chatService } from '$lib/services/ChatService';

vi.mock('$lib/services/ChatService', () => ({
    chatService: { ask: vi.fn() }
}));

describe('ChatVM', () => {
    it('should update messages and clear input on send', async () => {
        const vm = new ChatVM();
        vm.userInput = 'Tell me about carbon';

        const mockResponse = {
            answer: 'Carbon is...',
            sources: ["file1"]
        };

        vi.mocked(chatService.ask).mockResolvedValue(mockResponse as any);

        const promise = vm.sendMessage();

        // Immediate check
        expect(vm.userInput).toBe('');
        expect(vm.messages[0].content).toBe('Tell me about carbon');
        expect(vm.isLoading).toBe(true);

        await promise;

        // After AI responds
        expect(vm.messages).toHaveLength(2);
        expect(vm.messages[1].role).toBe('assistant');
        expect(vm.messages[1].sources).toHaveLength(1);
        expect(vm.isLoading).toBe(false);
    });

    it('should clear history', () => {
        const vm = new ChatVM();
        vm.messages = [{ role: 'user', content: 'Hi' }];
        vm.clearHistory();
        expect(vm.messages).toHaveLength(0);
    });
});