namespace DshEtlSearch.Api.Models.Requests;

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int Limit { get; set; } = 5;
    public float MinimumScore { get; set; } = 0.5f;
}
