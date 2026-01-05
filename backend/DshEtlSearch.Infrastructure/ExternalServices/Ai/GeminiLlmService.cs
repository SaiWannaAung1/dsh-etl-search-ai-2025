using Google.GenAI;
using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Interfaces.Infrastructure;
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
}