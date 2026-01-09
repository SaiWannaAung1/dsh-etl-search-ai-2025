import { chatService } from '$lib/services/ChatService';
import type { ChatMessage } from '$lib/models/Chat';

export class ChatVM {
    // Reactive State (Runes)
    messages = $state<ChatMessage[]>([]);
    userInput = $state('');
    isLoading = $state(false);

    async sendMessage() {
        if (!this.userInput.trim() || this.isLoading) return;

        const text = this.userInput;
        this.userInput = ''; // Clear input immediately for UX
        this.isLoading = true;

        // 1. Add user message to local state
        this.messages.push({ role: 'user', content: text });

        try {
            // 2. Call Service with current message and history
            const response = await chatService.ask({
                message: text,
                history: this.messages.slice(0, -1)
            });

            // 3. Update state with AI response
            this.messages.push({
                role: 'assistant',
                content: response.answer,
                sources: response.sources
            });
        } catch (error) {
            this.messages.push({
                role: 'assistant',
                content: "Error: Could not reach the research assistant."
            });
        } finally {
            this.isLoading = false;
        }
    }

    /**
     * NEW: Clears the entire chat history and resets state
     */
    clearHistory() {
        this.messages = [];    // Reassigning triggers reactivity in Svelte 5
        this.userInput = '';   // Optional: clear the current input too
        this.isLoading = false; // Ensure loading state is reset
        console.log("Chat history cleared");
    }
    
}