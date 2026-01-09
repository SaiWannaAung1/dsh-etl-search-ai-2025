export interface ChatMessage {
    role: 'user' | 'assistant';
    content: string;
    sources?: SourceFileResponse[]; // Only present for assistant messages
}

export interface SourceFileResponse {
    fileName: string;
    storagePath: string;
}

export interface ChatRequest {
    message: string;
    history: ChatMessage[];
}

export interface ChatResponse {
    answer: string;
    sources: SourceFileResponse[];
}