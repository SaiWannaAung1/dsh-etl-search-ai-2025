using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Interfaces.Infrastructure;

namespace DshEtlSearch.Infrastructure.ExternalServices.Ceh;

public class CehCatalogueClient : ICehCatalogueClient
{
    private readonly HttpClient _httpClient;
    
    // Base URLs
    private const string CatalogueBaseUrl = "https://catalogue.ceh.ac.uk";
    private const string DataPackageBaseUrl = "https://data-package.ceh.ac.uk";

    public CehCatalogueClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DshEtlSearch-Bot/1.0");
    }

    public async Task<Result<Stream>> GetMetadataAsync(string fileIdentifier, MetadataFormat format)
    {
        string url = BuildMetadataUrl(fileIdentifier, format);
        return await DownloadStreamAsync(url);
    }

    public async Task<Result<Stream>> DownloadDatasetZipAsync(string fileIdentifier)
    {
        // Pattern: https://data-package.ceh.ac.uk/data/{id}.zip
        string url = $"{DataPackageBaseUrl}/data/{fileIdentifier}.zip";
        
        // FIX: Call DownloadStreamAsync directly. 
        // Previously, you were passing the URL into DownloadSupportingDocsAsync, 
        // which treated the URL as an ID and broke the link.
        return await DownloadStreamAsync(url);
    }
    
    // Kept this as a public method if you need it later, but ensured it's not called by mistake above
    public async Task<Result<Stream>> DownloadSupportingDocsAsync(string fileIdentifier)
    {
        string url = $"{DataPackageBaseUrl}/sd/{fileIdentifier}.zip";
        return await DownloadStreamAsync(url);
    }  
    
    // public async Task<Result<Stream>> DownloadDatasetAsync(string fileIdentifier)
    // {
    //     string url = $"{CatalogueBaseUrl}/datastore/eidchub/{fileIdentifier}"; 
    //     return await DownloadStreamAsync(url);
    // }

    private string BuildMetadataUrl(string id, MetadataFormat format)
    {
        return format switch
        {
            MetadataFormat.Iso19115Xml => $"{CatalogueBaseUrl}/documents/gemini/waf/{id}.xml",
            MetadataFormat.JsonExpanded => $"{CatalogueBaseUrl}/documents/{id}?format=json",
            MetadataFormat.SchemaOrgJsonLd => $"{CatalogueBaseUrl}/documents/{id}?format=schema.org",
            MetadataFormat.RdfTurtle => $"{CatalogueBaseUrl}/documents/{id}?format=ttl",
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };
    }

    private async Task<Result<Stream>> DownloadStreamAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            
            if (!response.IsSuccessStatusCode)
            {
                return Result<Stream>.Failure($"Failed to fetch {url}. Status: {response.StatusCode}");
            }

            return Result<Stream>.Success(await response.Content.ReadAsStreamAsync());
        }
        catch (Exception ex)
        {
            return Result<Stream>.Failure($"Network error accessing {url}: {ex.Message}");
        }
    }
    
    public async Task<Result<string>> GetDirectoryListingHtmlAsync(string fileIdentifier)
    {
        // The directory listing is usually at the ResourceUrl found in the metadata
        string url = $"{CatalogueBaseUrl}/datastore/eidchub/{fileIdentifier}/"; 
        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) return Result<string>.Failure("Directory not accessible");
        

        string html =  response.Content.ReadAsStringAsync().Result;
        return Result<string>.Success(html);
    }
}