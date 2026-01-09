// src/lib/services/ChatService.ts
import { BaseService } from './BaseService';
import type {ChatRequest, ChatResponse} from "$lib/models/Chat";

export class ChatService extends BaseService {
    /**
     * Sends a chat request to the RAG backend.
     * Uses the 'post' method inherited from BaseService.
     */
    async ask(request: ChatRequest): Promise<ChatResponse> {
        return await this.post<ChatResponse>('/Chat/ask', request);
    }

  
}

// Export a single instance to be used across the app
export const chatService = new ChatService();