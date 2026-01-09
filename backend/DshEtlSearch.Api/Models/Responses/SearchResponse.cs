namespace DshEtlSearch.Api.Models.Responses;

public class SearchResponse
{
    public Guid DatasetId { get; set; } 
    public string? DocumentId { get; set; } // Added DocumentId 
    public string Authors { get; set; } = string.Empty; // Replaced FileName with Authors
    public string PreviewAbstract { get; set; } = string.Empty; // Replaced Snippet with Abstract
    public string Title { get; set; } = string.Empty;
    public float ConfidenceScore { get; set; }
}
