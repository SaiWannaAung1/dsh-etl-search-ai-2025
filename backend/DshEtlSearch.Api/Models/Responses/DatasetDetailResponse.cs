namespace DshEtlSearch.Api.Models.Responses;

public class DatasetDetailResponse
{
    public Guid DatasetId { get; set; }
    public string DocumentId { get; set; } = string.Empty; // Mapping FileIdentifier
    public string Title { get; set; } = string.Empty;
    public string? Abstract { get; set; }
    public string Authors { get; set; } = string.Empty;
    public string? Keywords { get; set; }
    public string? ResourceUrl { get; set; }
    public DateTime? PublishedDate { get; set; }
    public DateTime IngestedAt { get; set; }
}