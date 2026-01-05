using DshEtlSearch.Api.Models.Requests;
using DshEtlSearch.Api.Models.Responses;
using DshEtlSearch.Core;
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
    private readonly ILlmService _llmService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILlmService llmService,
        ILogger<ChatController> logger) 
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _llmService = llmService;
        _logger = logger;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<ChatResponse>> Ask([FromBody] ChatRequest request)
    {
        try
        {
            // 1. Retrieve Context (The "R" in RAG)
            var vectorResult = await _embeddingService.GenerateEmbeddingAsync(request.Message);
            var searchHits = await _vectorStore.SearchAsync("research_data", vectorResult.Value!, request.MaxContextChunks);

            var validHits = searchHits.Where(h => h.Score >= request.SimilarityThreshold).ToList();
            
            // 2. Combine snippets from your extracted DOCX/PDF files
            var context = string.Join("\n\n", validHits.Select(h => h.Text));

            if (string.IsNullOrWhiteSpace(context))
            {
                return Ok(new ChatResponse { Answer = "I couldn't find any relevant research data to answer that." });
            }

            // 3. Generate Answer (The "G" in RAG)
            var aiResult = await _llmService.GenerateAnswerAsync(request.Message, context);

            return Ok(new ChatResponse
            {
                Answer = aiResult.Value ?? "Error generating answer.",
                Sources = validHits.Select(h => h.SourceId.ToString()).Distinct().ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG Chat failure");
            return StatusCode(500, "The AI assistant is currently unavailable.");
        }
    }
}