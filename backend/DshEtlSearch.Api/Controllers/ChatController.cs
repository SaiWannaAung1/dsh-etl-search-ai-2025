using System.Text;
using DocumentFormat.OpenXml.Office2010.Ink;
using DshEtlSearch.Api.Models.Requests;
using DshEtlSearch.Api.Models.Responses;
using DshEtlSearch.Core;
using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace DshEtlSearch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly IMetadataRepository _repository;
    private readonly ILlmService _llmService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILlmService llmService,
        IMetadataRepository repository,
        ILogger<ChatController> logger) 
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _llmService = llmService;
        _repository = repository;
        _logger = logger;
    }
[HttpPost("ask")]
    public async Task<ActionResult<ChatResponse>> Ask([FromBody] ChatRequest request)
    {
        try
        {
            // 1. REWRITE: Create a standalone query from history (e.g., "Show me its files" -> "Show files for Pig Dataset")
            string standaloneQuery = await SimplifyQueryWithHistoryAsync(request.Message, request.History);

            // 2. INTENT: Is the user asking for the individual file list?
            bool isGranular = await IsGranularRequestAsync(standaloneQuery);

            // 3. SEARCH: Get relevant context from Vector Store
            var vectorResult = await _embeddingService.GenerateEmbeddingAsync(standaloneQuery);
            var searchHits = await _vectorStore.SearchAsync("research_data", vectorResult.Value!, request.MaxContextChunks);
            var validHits = searchHits.Where(h => h.Score >= request.SimilarityThreshold).ToList();

            var chatResponse = new ChatResponse();
            var contextBuilder = new StringBuilder();

            foreach (var hit in validHits)
            {
                // Logic Switch: Individual Files vs. Dataset Zip
                if (isGranular)
                {
                    var spec = new DataFilesByDatasetIdSpecification(hit.SourceId);
                    var files = await _repository.ListFilesAsync(spec);
                    if (files != null)
                    {
                        foreach (var f in files)
                        {
                            chatResponse.Sources.Add(new SourceFileResponse(f.FileName, f.StoragePath ) );
                        }
                    }
                }
                else
                {
                    var dataset = await _repository.GetByIdAsync(hit.SourceId);
                    if (!string.IsNullOrEmpty(dataset?.ResourceUrl))
                    {
                        chatResponse.Sources.Add(new SourceFileResponse ("Full Dataset (.zip)", dataset.ResourceUrl) );
                    }
                }
                contextBuilder.AppendLine(hit.Text);
            }

            // 4. GENERATE: Final answer using history for continuity
            var finalAiResult = await _llmService.GenerateAnswerAsync(request.Message, request.History, contextBuilder.ToString());
            
            chatResponse.Answer = finalAiResult.Value ?? "Below is the information regarding your request:";
            
            // 5. CLEANUP: Group by StoragePath to remove duplicate links
            chatResponse.Sources = chatResponse.Sources
                .Where(s => !string.IsNullOrEmpty(s.StoragePath))
                .GroupBy(s => s.StoragePath)
                .Select(g => g.First())
                .ToList();

            return Ok(chatResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat Failure");
            return StatusCode(500, "Assistant Unavailable");
        }
    }


private async Task<string> SimplifyQueryWithHistoryAsync(string message, List<ChatMessage> history)
    {
        if (history == null || !history.Any()) return message;

        var prompt = "Given the following conversation history and a follow-up question, rephrase the follow-up to be a standalone search query.\n" +
                     $"History: {string.Join(" | ", history.TakeLast(3).Select(h => h.Content))}\n" +
                     $"Follow-up: {message}\n" +
                     "Standalone query:";

        var result = await _llmService.GenerateAnswerAsync(prompt, "");
        return result.Value ?? message;
    }

    private async Task<bool> IsGranularRequestAsync(string message)
    {
        var prompt = "Determine if the user wants to see a list of individual files, specific documents, or separate data entries. " +
                     "Reply with 'YES' or 'NO' only.\n" +
                     $"Message: {message}";

        var result = await _llmService.GenerateAnswerAsync(prompt, "");
        return result.Value?.Contains("YES", StringComparison.OrdinalIgnoreCase) ?? false;
    }

}