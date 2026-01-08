namespace DshEtlSearch.Core.Interfaces.Infrastructure;

public interface IGoogleDriveService
{
    // Returns the WebContentLink or FileId for the uploaded file
    Task<(string FolderDownloadLink, string FileDownloadLink)> UploadFileToDocumentFolderAsync(
        string fileName, 
        string content, 
        string documentId, 
        CancellationToken token);    
}