namespace DshEtlSearch.Api.Models.Requests;

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public int MaxContextChunks { get; set; } = 3;
    public float SimilarityThreshold { get; set; } = 0.5f;
}