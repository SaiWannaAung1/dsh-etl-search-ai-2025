namespace DshEtlSearch.Core.Common;

public class VectorSearchResult
{
    // The ID of the Dataset this chunk belongs to (Guid)
    public Guid SourceId { get; set; }

    // The actual text snippet (the 200 words we embedded)
    public string Text { get; set; } = string.Empty;

    // The similarity score (1.0 is a perfect match, 0.0 is no match)
    public float Score { get; set; }

    // Extra data like "file_name" or "page_number"
    public Dictionary<string, string> Metadata { get; set; } = new();

    public VectorSearchResult(Guid sourceId, string text, float score)
    {
        SourceId = sourceId;
        Text = text;
        Score = score;
    }
}