using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DshEtlSearch.Core.Features.Ingestion;

public class EtlOrchestrator : IEtlService
{
    private readonly ICehCatalogueClient _cehClient;
    private readonly IArchiveProcessor _archiveProcessor;
    private readonly IMetadataRepository _repository;
    private readonly IMetadataParserFactory _parserFactory;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<EtlOrchestrator> _logger;
    private readonly IGoogleDriveService _googleDriveService;
    private const string StorageRoot = "DatasetStorage";
    private const string VectorCollectionName = "research_data"; 

    public EtlOrchestrator(
        ICehCatalogueClient cehClient,
        IArchiveProcessor archiveProcessor,
        IMetadataRepository repository,
        IMetadataParserFactory parserFactory,
        IEmbeddingService embeddingService, 
        IVectorStore vectorStore,
        ILogger<EtlOrchestrator> logger,
        IGoogleDriveService googleDriveService) // <--- Add this parameter
    {
        _cehClient = cehClient;
        _archiveProcessor = archiveProcessor;
        _repository = repository;
        _parserFactory = parserFactory;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _logger = logger;
        _googleDriveService = googleDriveService; // <--- Assign it here
    }

    public async Task RunBatchIngestionAsync(CancellationToken token = default)
    {
        try 
        {
            await _vectorStore.CreateCollectionAsync(VectorCollectionName, _embeddingService.VectorSize, token);
        }
        catch (Exception ex) { _logger.LogError(ex, "Vector store initialization failed."); }

        var identifierFile = "metadata-file-identifiers.txt";
        var ids = File.Exists(identifierFile) 
            ? (await File.ReadAllLinesAsync(identifierFile, token)).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
            : new List<string> { "bf82cec2-5f8a-407c-bf74-f8689ca35e83" };

        foreach (var id in ids)
        {
            if (token.IsCancellationRequested) break;
            await IngestDatasetAsync(id, token);
        }
    }

    public async Task<Result> IngestDatasetAsync(string fileIdentifier, CancellationToken token = default)
    {
        try
        {
            if (await _repository.ExistsAsync(fileIdentifier)) return Result.Success();

               var formats = new[] 
            { 
                MetadataFormat.Iso19115Xml,     
                MetadataFormat.JsonExpanded, 
                MetadataFormat.RdfTurtle, 
                MetadataFormat.SchemaOrgJsonLd 
            };

            Dataset? dataset = null;

            foreach (var format in formats)
            {
                try
                {
                    var metaResult = await _cehClient.GetMetadataAsync(fileIdentifier, format);
                    if (!metaResult.IsSuccess)
                    {
                        if (format == MetadataFormat.Iso19115Xml) 
                            return Result.Failure($"Primary metadata download failed: {metaResult.Error}");
                        continue; 
                    }

                    using var reader = new StreamReader(metaResult.Value!);
                    var content = await reader.ReadToEndAsync(token);

                    if (format == MetadataFormat.Iso19115Xml)
                    {
                        using var parseStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                        var parser = _parserFactory.GetParser(MetadataFormat.Iso19115Xml);
                        
                        var parseResult = parser.Parse(parseStream);
                        if (!parseResult.IsSuccess) 
                            return Result.Failure($"Primary parsing failed: {parseResult.Error}");

                        var dto = parseResult.Value!;
                        
                        dataset = new Dataset(fileIdentifier, dto.Title)
                        {
                            Abstract = dto.Abstract,
                            Authors = JsonSerializer.Serialize(dto.Authors),
                            PublishedDate = dto.PublishedDate,
                            Keywords = dto.Keywords,
                            ResourceUrl = dto.ResourceUrl
                        };
                    }
                    dataset?.AddRawMetadata(format.ToString(), content);
                }
                catch (Exception ex)
                {
                    if (format == MetadataFormat.Iso19115Xml)
                        return Result.Failure($"Critical error processing XML: {ex.Message}");
                }
            }

            if (dataset == null) return Result.Failure("Failed to initialize dataset (Primary XML missing).");

           

          
        // 2. PRIMARY DATASET DOWNLOAD & TARGETED EXTRACTION
        // =========================================================
          
            var primaryDataResult = await _cehClient.DownloadDatasetZipAsync(fileIdentifier);
            if (primaryDataResult.IsSuccess)
            {
                using var zipStream = primaryDataResult.Value!;

// 1. Extract content from the ZIP (remains the same)
                var dataExtraction = await _archiveProcessor.ExtractDocumentsAsync(zipStream, dataset.Id);

                if (dataExtraction.IsSuccess)
                {
                    foreach (var extractedFile in dataExtraction.Value!)
                    {
                        // 2. Filter for files specifically in the 'data/' folder
                        if (extractedFile.FileName.ToLower().Contains("data/"))
                        {
                            var safeFileName = Path.GetFileName(extractedFile.FileName);
                            var textContent = extractedFile.ExtractedText ?? "";

                            try
                            {
                                // 3. Convert extracted text to a Stream for Google Drive

                                // 4. UPLOAD TO GOOGLE DRIVE
                                // We pass "text/plain" as the MIME type for extracted text files
                                var driveUrl = await _googleDriveService.UploadFileToDocumentFolderAsync(
                                    safeFileName, 
                                    textContent, 
                                    dataset.FileIdentifier,
                                    token
                                );
                                // 5. Create the DataFile object using the Google Drive Public Link
                                var dataDoc = new DataFile(dataset.Id, safeFileName)
                                {
                                    StoragePath = driveUrl.FileDownloadLink, // This is now the webViewLink (e.g. https://drive.google.com/...)
                                    ExtractedText = textContent
                                };

                                // 6. Track document in the Dataset entity
                                dataset.AddDocument(dataDoc);

                                _logger.LogInformation($"✅ Successfully uploaded primary data to Google Drive: {safeFileName}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"❌ Failed to upload {safeFileName} to Google Drive.");
                                // Depending on requirements, you might want to 'continue' or 'return Result.Failure'
                            }
                        }
                    }
                }
            }
            else
            {
    
                // 1. Fetch the HTML directory listing
                var htmlResult = await _cehClient.GetDirectoryListingHtmlAsync(fileIdentifier);

                if (htmlResult.IsSuccess)
                {
                    string htmlContent = htmlResult.Value!;
        
                    // 2. Web Scrape file names using Regex 
                    // This looks for href links that aren't the "Parent Directory"
                    var fileMatches = Regex.Matches(htmlContent, @"<a href=""([^""]+?)"">([^<]+?\.[a-zA-Z0-9]{2,4})</a>");
                    foreach (Match match in fileMatches)
                    {
                        
                        var relativeUrl = match.Groups[1].Value; // e.g., "REDFIRE_Dose_Estimates.csv"
                        var fileName = match.Groups[2].Value.Trim();

                        // Skip navigation links
                        if (fileName.Contains("Parent Directory") || string.IsNullOrEmpty(fileName))
                            continue;

                        // 3. Save only the Metadata to Database (No Download)
                        var dataDoc = new DataFile(dataset.Id, fileName)
                        {
                            // We store the full URL in the NotMapped StoragePath for the UI to use
                            StoragePath = $"{dataset.ResourceUrl}/{relativeUrl}",
                            ExtractedText = "Remote file - metadata only"
                        };

                        dataset.AddDocument(dataDoc);
                        _logger.LogInformation($"Scraped metadata for remote file: {fileName}");
                    }
                }
            }

            // =========================================================
            // 3. SUPPORTING DOCS (Download + Vectorize)
            // =========================================================
            var supportingDocsResult = await _cehClient.DownloadSupportingDocsAsync(fileIdentifier);
            if (supportingDocsResult.IsSuccess)
            {
                using var zipStream = supportingDocsResult.Value!;
                var extractResult = await _archiveProcessor.ExtractDocumentsAsync(zipStream, dataset.Id);
                
                if (extractResult.IsSuccess)
                {
                    var vectorsToSave = new List<EmbeddingVector>();
                    foreach (var doc in extractResult.Value!)
                    {
                     
                        // Vectorize supporting documentation
                        if (!string.IsNullOrWhiteSpace(doc.ExtractedText))
                        {
                            // 1. Prepare the Contextual Text for Embedding
// Combining Metadata + Content ensures the vector is "aware" of its context
                            var enrichmentBuilder = new StringBuilder();
                            enrichmentBuilder.AppendLine($"Title: {dataset.Title}");
                            enrichmentBuilder.AppendLine($"Abstract: {dataset.Abstract}");
                            enrichmentBuilder.AppendLine($"Authors: {dataset.Authors}");
                            enrichmentBuilder.AppendLine($"Keywords: {dataset.Keywords}");
                            enrichmentBuilder.AppendLine($"Content: {doc.ExtractedText}");

                            var fullText = enrichmentBuilder.ToString();

// 2. Truncate for the Model (usually models have token limits)
                            var textToEmbed = fullText.Length > 1000 ? fullText[..1000] : fullText;

// 3. Generate Embedding
                            var embedResult = await _embeddingService.GenerateEmbeddingAsync(textToEmbed, token);

                            if (embedResult.IsSuccess)
                            {
                                // 4. Create Vector Object
                                // Note: We still pass the original 'doc.ExtractedText' to 'textContent' 
                                // so that we can show the actual file snippet in the UI search results.
                                var vector = new EmbeddingVector(
                                    dataset.Id, 
                                    VectorSourceType.DocumentContent, 
                                    doc.ExtractedText, 
                                    embedResult.Value!,
                                    dataset.Title, 
                                    dataset.Abstract, 
                                    dataset.Authors, 
                                    dataset.Keywords
                                );

                                vectorsToSave.Add(vector);
                            }
                        }
                       
                    }

                    if (vectorsToSave.Any())
                    {
                        await _vectorStore.UpsertVectorsAsync(VectorCollectionName, vectorsToSave, token);
                        _logger.LogInformation($"✅ Successfully vectorized {vectorsToSave.Count} supporting documents.");
                    }
                }
            }

            // 4. PERSIST TO SQL
            dataset.Authors = JsonSerializer.Deserialize<string>(dataset.Authors);
            await _repository.AddAsync(dataset);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ingestion failed for {fileIdentifier}");
            return Result.Failure(ex.Message);
        }
    }

    private async Task<Dataset?> ParsePrimaryMetadata(string id, CancellationToken token)
    {
        var metaResult = await _cehClient.GetMetadataAsync(id, MetadataFormat.Iso19115Xml);
        if (!metaResult.IsSuccess) return null;

        var parser = _parserFactory.GetParser(MetadataFormat.Iso19115Xml);
        var parseResult = parser.Parse(metaResult.Value!);
        
        if (!parseResult.IsSuccess) return null;
        var dto = parseResult.Value!;

        return new Dataset(id, dto.Title)
        {
            Abstract = dto.Abstract,
            Authors = JsonSerializer.Serialize(dto.Authors),
            Keywords = dto.Keywords,
            ResourceUrl = dto.ResourceUrl,
            PublishedDate = dto.PublishedDate
        };
    }
}