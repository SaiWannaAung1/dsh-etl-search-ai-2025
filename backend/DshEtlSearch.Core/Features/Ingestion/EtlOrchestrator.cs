using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Text;

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
    
    private const string StorageRoot = "DatasetStorage";
    private const string VectorCollectionName = "research_data"; 

    public EtlOrchestrator(
        ICehCatalogueClient cehClient,
        IArchiveProcessor archiveProcessor,
        IMetadataRepository repository,
        IMetadataParserFactory parserFactory,
        IEmbeddingService embeddingService, 
        IVectorStore vectorStore,           
        ILogger<EtlOrchestrator> logger)    
    {
        _cehClient = cehClient;
        _archiveProcessor = archiveProcessor;
        _repository = repository;
        _parserFactory = parserFactory;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task RunBatchIngestionAsync(CancellationToken token = default)
    {
        // 1. Initialize Vector Database (Create collection if missing)
        try 
        {
            _logger.LogInformation($"Checking/Creating Qdrant Collection: {VectorCollectionName}...");
            await _vectorStore.CreateCollectionAsync(VectorCollectionName, _embeddingService.VectorSize, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize vector collection. Search may not work.");
        }

        var identifierFile = "metadata-file-identifiers.txt";
        List<string> ids = File.Exists(identifierFile) 
            ? (await File.ReadAllLinesAsync(identifierFile, token)).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
            : new List<string> { "ba208b6c-6f1a-43b1-867d-bc1adaff6445" };

        foreach (var id in ids)
        {
            if (token.IsCancellationRequested) break;
            // FIX: Pass the token down to the method
            await IngestDatasetAsync(id, token);
        }
    }

    // FIX: Added CancellationToken to method signature
    public async Task<Result> IngestDatasetAsync(string fileIdentifier, CancellationToken token = default)
    {
        try
        {
            // A. Check duplicate
            if (await _repository.ExistsAsync(fileIdentifier)) 
            {
                _logger.LogInformation($"Skipping {fileIdentifier} (Already exists in SQL)");
                return Result.Success(); 
            }

            // =========================================================
            // B. METADATA PROCESSING
            // =========================================================
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
            // C. DOWNLOAD, EXTRACT & VECTORIZE
            // =========================================================
            var zipResult = await _cehClient.DownloadDatasetZipAsync(fileIdentifier);
            if (!zipResult.IsSuccess)
            {
                zipResult = await _cehClient.DownloadSupportingDocsAsync(fileIdentifier);

            }

            if (zipResult.IsSuccess)
            {
                using var zipStream = zipResult.Value!;
                var extractResult = await _archiveProcessor.ExtractDocumentsAsync(zipStream, dataset.Id);
                
                if (extractResult.IsSuccess)
                {
                    var localFolder = Path.Combine(StorageRoot, dataset.Id.ToString());
                    Directory.CreateDirectory(localFolder);

                    // List to hold vectors for batch upload
                    var vectorsToSave = new List<EmbeddingVector>();
                  
                    foreach (var doc in extractResult.Value!)
                    {
                        var validDocs = new[] { ".pdf", ".docx", ".json", ".html", ".txt", ".xml", ".ttl" };
                        string extension = Path.GetExtension(doc.FileName).ToLower();
                        
                        if (!string.IsNullOrEmpty(doc.ExtractedText) && validDocs.Contains(extension) )
                        {
                            // 1. Save File to Disk
                            var safeName = Path.GetFileName(doc.FileName); 
                            var fullPath = Path.Combine(localFolder, safeName);
                            await File.WriteAllTextAsync(fullPath, doc.ExtractedText, token);
                            doc.StoragePath = fullPath;

                            // 2. RESTORED: Generate Embedding
                            try 
                            {
                                // Limit text length for tokenization speed/safety
                                var textToEmbed = doc.ExtractedText.Length > 1000 
                                    ? doc.ExtractedText[..1000] 
                                    : doc.ExtractedText;

                                var embedResult = await _embeddingService.GenerateEmbeddingAsync(textToEmbed, token);
                                
                                if (embedResult.IsSuccess)
                                {
                                    var vector = new EmbeddingVector(
                                        sourceId: dataset.Id, 
                                        type: VectorSourceType.DocumentContent,
                                        text: textToEmbed,
                                        vector: embedResult.Value!
                                    );
                                   
                                    _logger.LogInformation(
                                        "✅ Vector Created for file '{FileName}' | ID: {Id} | First 3 Dims: [{V1:F4}, {V2:F4}, {V3:F4} ...]",
                                        doc.FileName,
                                        vector.Id,
                                        vector.Vector[0], vector.Vector[1], vector.Vector[2]
                                    );
                                    // 👆 -------------------------- 👆

                                    vectorsToSave.Add(vector);
            
                                    // Optional: Double check the count immediately
                                    _logger.LogInformation($"Current Vector Batch Count: {vectorsToSave.Count}");
                                    
                                }
                                else
                                {
                                    _logger.LogWarning($"Embedding failed for {doc.FileName}: {embedResult.Error}");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, $"Error generating embedding for {doc.FileName}");
                            }
                        }
                        
                        dataset.AddDocument(doc);
                    }

                    // 3. RESTORED: Upsert to Qdrant
                    if (vectorsToSave.Any())
                    {
                        _logger.LogInformation($"Upserting {vectorsToSave.Count} vectors to Qdrant...");
                        await _vectorStore.UpsertVectorsAsync(VectorCollectionName, vectorsToSave, token);
                        _logger.LogInformation("✅ Qdrant upsert complete.");
                    }
                    else
                    {
                         _logger.LogWarning($"⚠️ No vectors were generated for dataset {fileIdentifier}. Qdrant was not updated.");
                    }
                }
            }

            // D. Save Metadata to SQL Database
            await _repository.AddAsync(dataset);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ETL Error");
            return Result.Failure($"ETL Error: {ex.Message}");
        }
    }
}