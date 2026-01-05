using System.IO.Compression;
using System.Text;
using DocumentFormat.OpenXml.Packaging; // <--- This requires the NuGet package
using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DshEtlSearch.Infrastructure.ExternalServices;

public class ArchiveProcessor : IArchiveProcessor
{
    private readonly ILogger<ArchiveProcessor> _logger;

    public ArchiveProcessor(ILogger<ArchiveProcessor> logger)
    {
        _logger = logger;
    }

    public async Task<Result<List<SupportingDocument>>> ExtractDocumentsAsync(Stream archiveStream, Guid datasetId)
    {
        var documents = new List<SupportingDocument>();

        try
        {
            using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read);

            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;

                var doc = new SupportingDocument(datasetId, entry.FullName);
                var extension = Path.GetExtension(entry.Name).ToLower();

                using (var entryStream = entry.Open())
                {
                    if (extension == ".docx")
                    {
                        doc.ExtractedText = ExtractTextFromDocx(entryStream);
                    }
                    else if (IsTextBased(extension))
                    {
                        using var reader = new StreamReader(entryStream, Encoding.UTF8);
                        doc.ExtractedText = await reader.ReadToEndAsync();
                    }
                }

                if (!string.IsNullOrWhiteSpace(doc.ExtractedText))
                {
                    documents.Add(doc);
                }
            }

            return Result<List<SupportingDocument>>.Success(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process zip archive");
            return Result<List<SupportingDocument>>.Failure(ex.Message);
        }
    }

    private string ExtractTextFromDocx(Stream entryStream)
    {
        try
        {
            // WordprocessingDocument needs a seekable stream
            using var ms = new MemoryStream();
            entryStream.CopyTo(ms);
            ms.Position = 0;

            // This is where WordprocessingDocument is used
            using var wordDoc = WordprocessingDocument.Open(ms, false);
            var body = wordDoc.MainDocumentPart?.Document.Body;
            
            // InnerText pulls only the text, leaving behind the XML tags and binary junk
            return body?.InnerText ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"DOCX extraction failed: {ex.Message}");
            return string.Empty;
        }
    }

    private bool IsTextBased(string ext) => 
        new[] { ".json", ".xml", ".html", ".txt", ".csv", ".ttl" }.Contains(ext);
}