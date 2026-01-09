using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Common.Enums;

namespace DshEtlSearch.Core.Interfaces.Infrastructure;

public interface ICehCatalogueClient
{
    /// <summary>
    /// Fetches the metadata document for a specific dataset ID in the requested format.
    /// URL Pattern 1 (ISO): /documents/gemini/waf/{id}.xml
    /// URL Pattern 2 (JSON): /documents/{id}?format=json
    /// </summary>
    Task<Result<Stream>> GetMetadataAsync(string fileIdentifier, MetadataFormat format);

    /// <summary>
    /// Downloads the main dataset package (Zip).
    /// URL Pattern: https://data-package.ceh.ac.uk/data/{id}.zip
    /// </summary>
    Task<Result<Stream>> DownloadDatasetZipAsync(string fileIdentifier);

    // Optional: Added this to match your implementation, in case other services need it
    Task<Result<Stream>> DownloadSupportingDocsAsync(string fileIdentifier);
    
    Task<Result<string>> GetDirectoryListingHtmlAsync(string fileIdentifier);

}