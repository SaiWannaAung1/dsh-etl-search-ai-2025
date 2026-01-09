using Google.GenAI;
using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using Google.GenAI.Types;
using Microsoft.Extensions.Configuration;

namespace DshEtlSearch.Infrastructure.ExternalServices.Ai;

public class GeminiLlmService : ILlmService
{
    private readonly Client _client;
    private const string ModelName = "gemini-2.5-flash"; // Latest fast model

    public GeminiLlmService(IConfiguration configuration)
    {
        var apiKey = configuration["Google:GeminiApiKey"];
        _client = new Client(apiKey: apiKey);
    }

    public async Task<Result<string>> GenerateAnswerAsync(string query, string context, CancellationToken token = default)
    {
        try
        {
            string prompt = $@"
            You are a research assistant for the CEH Catalogue. 
            Use the following snippets to answer the user's question accurately.
            If the answer isn't in the context, politely say you don't know based on the documents.

            CONTEXT:
            {context}

            USER QUESTION: 
            {query}

            ANSWER:";

            // The new SDK uses GenerateContentAsync
            var response = await _client.Models.GenerateContentAsync(
                model: ModelName,
                contents: prompt
            );

            // Navigate the response object to get the text
            var textResult = response.Candidates[0].Content.Parts[0].Text;
            
            return Result<string>.Success(textResult);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Gemini API error: {ex.Message}");
        }
    }
    
    public async Task<Result<string>> GenerateAnswerAsync(
        string query, 
        List<ChatMessage> history, 
        string context, 
        CancellationToken token = default)
    {
        try
        {
            // 1. Prepare the System/Context Instruction
            string systemInstruction = $@"
            You are a research assistant for the CEH Catalogue. 
            Use the following CONTEXT to answer the user's question accurately.
            If the answer isn't in the context, politely say you don't know based on the documents.
            
            CONTEXT:
            {context}";

            // 2. Build the message list for Gemini
            // We combine the context into the prompt or as a system instruction
            var contents = new List<Content>();

            // Add history to the content list
            foreach (var msg in history)
            {
                contents.Add(new Content 
                { 
                    Role = msg.Role.ToLower() == "assistant" ? "model" : "user", 
                    Parts = new List<Part> { new Part { Text = msg.Content } } 
                });
            }

            // Add the current query with the system instruction prefix
            contents.Add(new Content
            {
                Role = "user",
                Parts = new List<Part> { new Part { Text = $"{systemInstruction}\n\nUSER QUESTION: {query}" } }
            });

            // 3. Call Gemini
            var response = await _client.Models.GenerateContentAsync(
                model: ModelName,
                contents: contents
            );

            var textResult = response.Candidates[0].Content.Parts[0].Text;
            
            return Result<string>.Success(textResult);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Gemini API error: {ex.Message}");
        }
    }
    
}