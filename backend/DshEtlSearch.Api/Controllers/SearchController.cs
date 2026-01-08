using DshEtlSearch.Api.Models.Requests;
using DshEtlSearch.Api.Models.Responses;
using DshEtlSearch.Core.Common;
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

            // 1. Get search hits from Vector Store (Qdrant)
            // Note: We might request more than the 'Limit' initially because we will deduplicate them
            var rawResults = await _vectorStore.SearchAsync(CollectionName, vectorResult.Value!, request.Limit * 2);

            // 2. Group by SourceId to ensure unique datasets, picking the best score for each
            var uniqueHits = rawResults
                .GroupBy(h => h.SourceId)
                .Select(group => group.OrderByDescending(x => x.Score).First())
                .Take(request.Limit) // Now apply the actual user limit
                .ToList();

            var responseList = new List<SearchResponse>();

            foreach (var hit in uniqueHits)
            {
                var dataset = await _repository.GetByIdAsync(hit.SourceId);
                if (dataset == null) continue;

                var truncatedAbstract = TruncateWords(dataset.Abstract ?? "", 50);
                if (hit.Score > 0.49)
                {
                    responseList.Add(new SearchResponse
                    {
                        DatasetId = hit.SourceId,
                        // DocumentId is now a string representation of the specific vector point ID
                        DocumentId = dataset.FileIdentifier, 
                        Title = dataset.Title ?? "Unknown Dataset",
                        Authors = dataset.Authors ?? "Unknown Authors",
                        PreviewAbstract = truncatedAbstract,
                        ConfidenceScore = hit.Score
                    });
                }

               
            }

            return Ok(responseList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search error");
            return StatusCode(500, "Internal error");
        }
    }
    
    
    [HttpGet("{id}")]
    public async Task<ActionResult<DatasetDetailResponse>> GetById(Guid id)
    {
        try
        {
            // 1. Fetch from SQLite Repository
            var dataset = await _repository.GetByIdAsync(id);

            if (dataset == null)
            {
                _logger.LogWarning($"Dataset with ID {id} not found.");
                return NotFound(new { message = "Dataset not found." });
            }

            // 2. Map Domain Entity to Response DTO
            var response = new DatasetDetailResponse
            {
                DatasetId = dataset.Id,
                DocumentId = dataset.FileIdentifier, // Maps the internal ID/Code
                Title = dataset.Title,
                Abstract = dataset.Abstract,
                Authors = dataset.Authors ?? "Unknown",
                Keywords = dataset.Keywords,
                ResourceUrl = dataset.ResourceUrl,
                PublishedDate = dataset.PublishedDate,
                IngestedAt = dataset.IngestedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving dataset {id}");
            return StatusCode(500, "Internal server error while fetching details.");
        }
    }
    
    
    [HttpGet("{id}/files")]
    public async Task<ActionResult<List<DataFileResponse>>> GetFilesByDataSetId(Guid id)
    {
        try
        {
            _logger.LogInformation($"Retrieving file list for Dataset: {id}");

            // 1. Define the search rule using the Specification Pattern
            var spec = new DataFilesByDatasetIdSpecification(id);

            // 2. Fetch from the repository (SupportingDocuments table)
            var files = await _repository.ListFilesAsync(spec);

            if (files == null || !files.Any())
            {
                return NotFound(new { message = "No data files associated with this record." });
            }

            // 3. Map to the DTO to produce the name and resource link
            var response = files.Where(f=> f.DatasetId == id).Select(f => new DataFileResponse
            {
                FileName = f.FileName,
                StoragePath = f.StoragePath
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching files for {id}");
            return StatusCode(500, "Internal server error");
        }
    }
    
// Helper method to limit text to a specific word count
private string TruncateWords(string text, int wordCount)
{
    if (string.IsNullOrWhiteSpace(text)) return string.Empty;

    var words = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
    if (words.Length <= wordCount) return text;

    return string.Join(" ", words.Take(wordCount)) + "...";
}
    
    
    

    
}