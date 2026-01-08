namespace DshEtlSearch.Core.Interfaces.Infrastructure;

public interface IGoogleDriveService
{
    // Returns the WebContentLink or FileId for the uploaded file
    Task<string> UploadFileAsync(string fileName, string content, CancellationToken token);
    
}