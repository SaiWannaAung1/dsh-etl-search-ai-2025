using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json; // Fixes JsonDocument
using Google.Apis.Auth.OAuth2;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DshEtlSearch.Infrastructure.ExternalServices.GoogleDrive;

public class GoogleDriveService : IGoogleDriveService
{
    private readonly string _folderId;
    private readonly string _keyPath;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GoogleDriveService> _logger;

    public GoogleDriveService(
        IConfiguration config, 
        IHttpClientFactory httpClientFactory,
        ILogger<GoogleDriveService> logger)
    {
        _folderId = config["GoogleDrive:FolderId"] ?? throw new ArgumentNullException("GoogleDrive FolderId missing");
        _keyPath = "dshetl2025-8cd5a50f661b.json"; 
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

public async Task<(string FolderDownloadLink, string FileDownloadLink)> UploadFileToDocumentFolderAsync(
    string fileName, 
    string content, 
    string documentId, 
    CancellationToken token)
{
    // 1. Load Client Secrets & Authorize
    using var stream = new FileStream("client_secret_878010677150-i7imlkclejheqol0vm9e0nhl0pp5s1r6.apps.googleusercontent.com.json", FileMode.Open, FileAccess.Read);
    var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
        GoogleClientSecrets.FromStream(stream).Secrets,
        new[] { DriveService.Scope.DriveFile },
        "user",
        token,
        new FileDataStore("TokenStore", true));

    var service = new DriveService(new BaseClientService.Initializer
    {
        HttpClientInitializer = credential,
        ApplicationName = "DshEtlSearch"
    });

    // 2. GET OR CREATE THE FOLDER
    var folderMetadata = await GetOrCreateFolderMetadataAsync(service, documentId, _folderId, token);
    
    // ✅ FOLDER DOWNLOAD LINK: Google doesn't provide this via API for folders.
    // We construct the UC export link which triggers Google's "Zip and Download" UI.
    string folderDownloadLink = $"https://drive.google.com/uc?export=download&id={folderMetadata.Id}";

    // 3. UPLOAD THE FILE
    var fileMetadata = new Google.Apis.Drive.v3.Data.File()
    {
        Name = fileName,
        Parents = new List<string> { folderMetadata.Id }
    };

    byte[] byteArray = Encoding.UTF8.GetBytes(content);
    using var uploadStream = new MemoryStream(byteArray);

    var request = service.Files.Create(fileMetadata, uploadStream, "text/csv");
    
    // ✅ FILE DOWNLOAD LINK: Ask for 'webContentLink'
    request.Fields = "webContentLink";

    var result = await request.UploadAsync(token);

    if (result.Status == Google.Apis.Upload.UploadStatus.Failed)
        throw new Exception($"Upload failed: {result.Exception.Message}");

    // 4. Return the direct download links
    return (folderDownloadLink, request.ResponseBody.WebContentLink);
}

private async Task<Google.Apis.Drive.v3.Data.File> GetOrCreateFolderMetadataAsync(DriveService service, string folderName, string parentRootId, CancellationToken token)
{
    var listRequest = service.Files.List();
    listRequest.Q = $"name = '{folderName}' and mimeType = 'application/vnd.google-apps.folder' and '{parentRootId}' in parents and trashed = false";
    listRequest.Fields = "files(id)"; // We only need the ID to build the download link
    
    var response = await listRequest.ExecuteAsync(token);
    var existingFolder = response.Files.FirstOrDefault();

    if (existingFolder != null) return existingFolder;

    var folderMetadata = new Google.Apis.Drive.v3.Data.File()
    {
        Name = folderName,
        MimeType = "application/vnd.google-apps.folder",
        Parents = new List<string> { parentRootId }
    };

    var createRequest = service.Files.Create(folderMetadata);
    createRequest.Fields = "id"; 
    return await createRequest.ExecuteAsync(token);
}

}