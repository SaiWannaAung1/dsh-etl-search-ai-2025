using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Core.Interfaces.Services;
using System.Text;

namespace DshEtlSearch.Core.Features.Ingestion;

public class EtlOrchestrator : IEtlService
{
    private readonly ICehCatalogueClient _cehClient;
    private readonly IArchiveProcessor _archiveProcessor;
    private readonly IMetadataRepository _repository;
    private readonly IMetadataParserFactory _parserFactory;
    
    // Configurable root folder for local storage
    private const string StorageRoot = "DatasetStorage";

    public EtlOrchestrator(
        ICehCatalogueClient cehClient,
        IArchiveProcessor archiveProcessor,
        IMetadataRepository repository,
        IMetadataParserFactory parserFactory)
    {
        _cehClient = cehClient;
        _archiveProcessor = archiveProcessor;
        _repository = repository;
        _parserFactory = parserFactory;
    }

    public async Task RunBatchIngestionAsync(CancellationToken token = default)
    {
        var identifierFile = "metadata-file-identifiers.txt";
        List<string> ids = File.Exists(identifierFile) 
            ? (await File.ReadAllLinesAsync(identifierFile, token)).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
            : new List<string> { "ba208b6c-6f1a-43b1-867d-bc1adaff6445" };

        foreach (var id in ids)
        {
            if (token.IsCancellationRequested) break;
            await IngestDatasetAsync(id);
        }
    }

    public async Task<Result> IngestDatasetAsync(string fileIdentifier)
    {
        try
        {
            // A. Check duplicate
            if (await _repository.ExistsAsync(fileIdentifier)) return Result.Success(); 

            // =========================================================
            // B. METADATA PROCESSING (Unified Loop)
            // =========================================================
            var formats = new[] 
            { 
                MetadataFormat.Iso19115Xml,     // PRIMARY: Must be first!
                MetadataFormat.JsonExpanded, 
                MetadataFormat.RdfTurtle, 
                MetadataFormat.SchemaOrgJsonLd 
            };

            Dataset? dataset = null;

            foreach (var format in formats)
            {
                try
                {
                    // 1. Download
                    var metaResult = await _cehClient.GetMetadataAsync(fileIdentifier, format);
                    
                    if (!metaResult.IsSuccess)
                    {
                        // Critical failure only if it's the primary XML
                        if (format == MetadataFormat.Iso19115Xml) 
                            return Result.Failure($"Primary metadata download failed: {metaResult.Error}");
                        
                        continue; // Skip secondary formats if they fail
                    }

                    // 2. Read Content
                    using var reader = new StreamReader(metaResult.Value!);
                    var content = await reader.ReadToEndAsync();

                    // 3. Primary Handling (XML): Parse & Create Dataset
                    if (format == MetadataFormat.Iso19115Xml)
                    {
                        using var parseStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                        var parser = _parserFactory.GetParser(MetadataFormat.Iso19115Xml);
                        
                        var parseResult = parser.Parse(parseStream);
                        if (!parseResult.IsSuccess) 
                            return Result.Failure($"Primary parsing failed: {parseResult.Error}");

                        var dto = parseResult.Value!;
                        
                        // Create the Entity
                        dataset = new Dataset(fileIdentifier, dto.Title)
                        {
                            Abstract = dto.Abstract,
                            Authors = dto.Authors,
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

            // =========================================================
            // C. DOWNLOAD & EXTRACT FILES (Local Storage)
            // =========================================================
            var zipResult = await _cehClient.DownloadDatasetZipAsync(fileIdentifier);
            if (zipResult.IsSuccess)
            {
                using var zipStream = zipResult.Value!;
                var extractResult = await _archiveProcessor.ExtractDocumentsAsync(zipStream, dataset.Id);
                
                if (extractResult.IsSuccess)
                {
                    // Ensure local folder exists: DatasetStorage/{GUID}
                    var localFolder = Path.Combine(StorageRoot, dataset.Id.ToString());
                    Directory.CreateDirectory(localFolder);

                    foreach (var doc in extractResult.Value!)
                    {
                        // Save physical file to disk
                        if (!string.IsNullOrEmpty(doc.ExtractedText))
                        {
                            var safeName = Path.GetFileName(doc.FileName); // prevent path traversal
                            var fullPath = Path.Combine(localFolder, safeName);
                            
                            await File.WriteAllTextAsync(fullPath, doc.ExtractedText);
                            
                            // Tell the DB where we put it
                            doc.StoragePath = fullPath;
                        }

                        dataset.AddDocument(doc);
                    }
                }
            }

            // D. Save to Database
            await _repository.AddAsync(dataset);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"ETL Error: {ex.Message}");
        }
    }
}