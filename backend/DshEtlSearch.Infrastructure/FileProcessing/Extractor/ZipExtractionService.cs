using System.IO.Compression;
using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DshEtlSearch.Infrastructure.FileProcessing.Extractor;

public class ZipExtractionService : IArchiveProcessor
{
    private readonly ILogger<ZipExtractionService> _logger;

    public ZipExtractionService(ILogger<ZipExtractionService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<List<SupportingDocument>>> ExtractDocumentsAsync(Stream archiveStream, Guid datasetId)
    {
        var documents = new List<SupportingDocument>();

        try
        {
            // Copy to MemoryStream to ensure it is seekable
            using var memoryStream = new MemoryStream();
            await archiveStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

            foreach (var entry in archive.Entries)
            {
                // Skip empty entries or directories
                if (string.IsNullOrEmpty(entry.Name) || entry.Length == 0) continue;

                string content = string.Empty;
                try 
                {
                    // Try to read content as text
                    content = await ReadEntryContentAsync(entry);
                }
                catch
                {
                    // If it's a binary file (image, exe, etc.), just save empty string
                    // We don't want to crash the whole process
                    content = string.Empty; 
                }

                // SIMPLIFIED: We just use 'FileType.Unknown' for everything.
                // We don't check extensions anymore.
                var doc = new SupportingDocument(datasetId, entry.Name, FileType.Unknown, entry.Length)
                {
                    ExtractedText = content
                };

                documents.Add(doc);
            }

            return Result<List<SupportingDocument>>.Success(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract zip archive");
            return Result<List<SupportingDocument>>.Failure($"Failed to extract archive: {ex.Message}");
        }
    }

    private async Task<string> ReadEntryContentAsync(ZipArchiveEntry entry)
    {
        using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);
        return await reader.ReadToEndAsync();
    }
}