using DshEtlSearch.Api.Models.Requests;
using DshEtlSearch.Api.Models.Responses;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace DshEtlSearch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly IMetadataRepository _repository;
    private readonly ILogger<SearchController> _logger;

    private const string CollectionName = "research_data";

    public SearchController(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        IMetadataRepository repository,
        ILogger<SearchController> logger)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _repository = repository;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<List<SearchResponse>>> Search([FromBody] SearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest("Query cannot be empty.");

        try
        {
            var vectorResult = await _embeddingService.GenerateEmbeddingAsync(request.Query);
            if (!vectorResult.IsSuccess) return StatusCode(500, "Embedding failed.");

            // Search returns the DTO with the Score
            var results = await _vectorStore.SearchAsync(CollectionName, vectorResult.Value!, request.Limit);

            var responseList = new List<SearchResponse>();

            foreach (var hit in results)
            {
                var dataset = await _repository.GetByIdAsync(hit.SourceId);
            
                responseList.Add(new SearchResponse
                {
                    DatasetId = hit.SourceId,
                    Title = dataset?.Title ?? "Unknown Dataset",
                    Snippet = hit.Text,
                    ConfidenceScore = hit.Score // <--- Success!
                });
            }

            return Ok(responseList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search error");
            return StatusCode(500, "Internal error");
        }
    }
    
}