namespace DshEtlSearch.Api.Models.Responses;

public class ChatResponse
{
    public string Answer { get; set; } = string.Empty;
    // It's good practice to show which DOCX/PDFs were used to answer
    public List<string> Sources { get; set; } = new(); 
}