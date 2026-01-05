namespace DshEtlSearch.Api.Models.Responses;

public class SearchResponse
{
    public Guid DatasetId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public float ConfidenceScore { get; set; }
    public string Title { get; set; } = string.Empty;
}
