using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Interfaces.Application;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Infrastructure.FileProcessing.Parsers; // For Factory
using Microsoft.Extensions.Logging;

namespace DshEtlSearch.Infrastructure.Services
{
    public class EtlOrchestrator : IEtlOrchestrator
    {
        private readonly IDownloader _downloader;
        private readonly IExtractionService _extractor;
        private readonly MetadataParserFactory _parserFactory;
        private readonly IMetadataRepository _repository;
        private readonly ILogger<EtlOrchestrator> _logger;

        public EtlOrchestrator(
            IDownloader downloader,
            IExtractionService extractor,
            MetadataParserFactory parserFactory,
            IMetadataRepository repository,
            ILogger<EtlOrchestrator> logger)
        {
            _downloader = downloader;
            _extractor = extractor;
            _parserFactory = parserFactory;
            _repository = repository;
            _logger = logger;
        }

        public async Task<Result<Guid>> ImportDatasetAsync(string datasetUrl)
        {
            // 1. Validation
            if (string.IsNullOrWhiteSpace(datasetUrl))
                return Result<Guid>.Failure("Dataset URL is required.");

            // 2. Check if already exists (Idempotency)
            // Note: In a real app, you might parse the ID from the URL or metadata first. 
            // Here, we check strictly by Source URL or Identifier if known. 
            // (Skipped for MVP to keep flow simple, but recommended in production).

            string tempFolder = Path.Combine(Path.GetTempPath(), "DshEtl_" + Guid.NewGuid());
            
            try
            {
                _logger.LogInformation("Starting ETL for: {Url}", datasetUrl);

                // 3. Download
                var downloadResult = await _downloader.DownloadStreamAsync(datasetUrl);
                if (!downloadResult.IsSuccess) return Result<Guid>.Failure(downloadResult.Error!);

                using var zipStream = downloadResult.Value!;

                // 4. Extract
                var extractResult = await _extractor.ExtractZipAsync(zipStream, tempFolder);
                if (!extractResult.IsSuccess) return Result<Guid>.Failure(extractResult.Error!);

                var extractedFiles = extractResult.Value!;
                if (!extractedFiles.Any()) return Result<Guid>.Failure("Zip archive was empty.");

                // 5. Detect Metadata File (Strategy Selection)
                var metadataFile = FindMetadataFile(extractedFiles);
                if (metadataFile == null) return Result<Guid>.Failure("No suitable metadata file (XML/JSON) found in archive.");

                // 6. Select Parser
                var format = DetectFormat(metadataFile);
                var parser = _parserFactory.GetParser(format);

                // 7. Parse
                using var fileStream = File.OpenRead(metadataFile);
                var parseResult = await parser.ParseAsync(fileStream);
                
                if (!parseResult.IsSuccess) return Result<Guid>.Failure($"Parsing failed: {parseResult.Error}");

                var metadataRecord = parseResult.Value!;

                // 8. Create & Save Domain Entity
                // We use the URL as the Source Identifier for now
                var dataset = new Dataset(datasetUrl)
                {
                    Metadata = metadataRecord
                };

                // Add references to all extracted files as supporting docs
                foreach (var file in extractedFiles)
                {
                    var fileInfo = new FileInfo(file);
                    dataset.AddDocument(fileInfo.Name, DetermineFileType(fileInfo.Extension), fileInfo.Length);
                    // In a real app, upload 'file' to Blob Storage here and update StoragePath
                }

                await _repository.AddAsync(dataset);
                await _repository.SaveChangesAsync();

                _logger.LogInformation("Successfully imported dataset {Id}", dataset.Id);
                return Result<Guid>.Success(dataset.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ETL Fatal Error");
                return Result<Guid>.Failure(ex.Message);
            }
            finally
            {
                // 9. Cleanup
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
            }
        }

        // --- Helper Methods ---

        private string? FindMetadataFile(List<string> files)
        {
            // Priority: metadata.xml > *.xml > *.json
            return files.FirstOrDefault(f => Path.GetFileName(f).Equals("metadata.xml", StringComparison.OrdinalIgnoreCase))
                   ?? files.FirstOrDefault(f => f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                   ?? files.FirstOrDefault(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
        }

        private MetadataFormat DetectFormat(string filePath)
        {
            if (filePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) return MetadataFormat.Iso19115Xml;
            if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) return MetadataFormat.JsonExpanded; // Defaulting to expanded for MVP
            throw new NotSupportedException("Unknown metadata format");
        }

        private FileType DetermineFileType(string extension)
        {
            return extension.ToLower() switch
            {
                ".xml" => FileType.Xml,
                ".json" => FileType.Json,
                ".pdf" => FileType.Pdf,
                ".zip" => FileType.Zip,
                ".txt" => FileType.Txt,
                _ => FileType.Unknown
            };
        }
    }
}