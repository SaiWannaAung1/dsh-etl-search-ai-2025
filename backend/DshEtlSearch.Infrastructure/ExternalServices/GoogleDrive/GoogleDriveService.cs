using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json; // Fixes JsonDocument
using Google.Apis.Auth.OAuth2;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using Google.Apis.Drive.v3;
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

    public async Task<string> UploadFileAsync(string fileName, string content, CancellationToken token)
    {
        // 1. Load Client Secrets (The OAuth JSON, NOT the Service Account JSON)
        using var stream = new FileStream("client_secret_878010677150-i7imlkclejheqol0vm9e0nhl0pp5s1r6.apps.googleusercontent.com.json", FileMode.Open, FileAccess.Read);
    
        // 2. Authorize as YOU (shopflex2001@gmail.com)
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromStream(stream).Secrets,
            new[] { DriveService.Scope.DriveFile },
            "user", // This saves a token locally so you don't login every time
            token,
            new FileDataStore("TokenStore", true));

        // 3. Initialize the Drive Service as a User
        var service = new DriveService(new Google.Apis.Services.BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "DshEtlSearch"
        });

        // 4. Create the File using YOUR storage quota
        var fileMetadata = new Google.Apis.Drive.v3.Data.File()
        {
            Name = fileName,
            Parents = new List<string> { _folderId }
        };

        byte[] byteArray = Encoding.UTF8.GetBytes(content);
        using var uploadStream = new MemoryStream(byteArray);
    
        var request = service.Files.Create(fileMetadata, uploadStream, "text/csv");
        request.Fields = "id";
    
        var result = await request.UploadAsync(token);
    
        if (result.Status == Google.Apis.Upload.UploadStatus.Failed)
            throw new Exception(result.Exception.Message);

        return $"https://drive.google.com/uc?id={request.ResponseBody.Id}&export=download";
    }
}