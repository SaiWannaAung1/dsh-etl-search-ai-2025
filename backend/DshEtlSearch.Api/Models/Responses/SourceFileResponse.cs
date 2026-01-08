namespace DshEtlSearch.Api.Models.Responses;

public class SourceFileResponse
{
    public string FileName { get; set; } = string.Empty;
    
    // This property must match exactly what you use in your LINQ (s.StoragePath)
    public string StoragePath { get; set; } = string.Empty;

    public SourceFileResponse(string fileName, string storagePath)
    {
        FileName = fileName;
        StoragePath = storagePath;
    }
}