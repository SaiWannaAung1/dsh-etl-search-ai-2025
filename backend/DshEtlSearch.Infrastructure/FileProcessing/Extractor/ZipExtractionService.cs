using System.IO.Compression;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using iText.Kernel.Pdf; // Add this for PDF
using iText.Kernel.Pdf.Canvas.Parser; // Add this for PDF
using iText.Kernel.Pdf.Canvas.Parser.Listener; // Add this for PDF
using DshEtlSearch.Core.Common;
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

    public async Task<Result<List<DataFile>>> ExtractDocumentsAsync(Stream archiveStream, Guid datasetId)
    {
        var documents = new List<DataFile>();

        try
        {
            using var memoryStream = new MemoryStream();
            await archiveStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

            foreach (var entry in archive.Entries)
            {
                // FIX: Check FullName for directory entries (entries ending in / are folders)
                if (string.IsNullOrEmpty(entry.Name) || entry.Length == 0) continue;

                string content = string.Empty;
                string extension = Path.GetExtension(entry.FullName).ToLower(); // Use FullName

                try 
                {
                    if (extension == ".docx")
                    {
                        content = ExtractTextFromDocx(entry);
                    }
                    else if (extension == ".pdf")
                    {
                        content = ExtractTextFromPdf(entry);
                    }
                    else if (IsTextFile(extension))
                    {
                        content = await ReadEntryContentAsync(entry);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Could not read {entry.FullName}: {ex.Message}");
                    content = string.Empty; 
                }

                // FIX: Pass entry.FullName here so it includes "supporting-documents/"
                var doc = new DataFile(datasetId, entry.FullName)
                {
                    ExtractedText = content
                };

                documents.Add(doc);
            }

            return Result<List<DataFile>>.Success(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract zip archive");
            return Result<List<DataFile>>.Failure(ex.Message);
        }
    }
    

    private string ExtractTextFromDocx(ZipArchiveEntry entry)
    {
        try
        {
            using var entryStream = entry.Open();
            using var ms = new MemoryStream();
            entryStream.CopyTo(ms);
            ms.Position = 0;

            using var wordDoc = WordprocessingDocument.Open(ms, false);
            var body = wordDoc.MainDocumentPart?.Document.Body;
            return body?.InnerText ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    private string ExtractTextFromPdf(ZipArchiveEntry entry)
    {
        try
        {
            using var entryStream = entry.Open();
            using var ms = new MemoryStream();
            entryStream.CopyTo(ms);
            ms.Position = 0;

            StringBuilder text = new StringBuilder();
            using (var pdfReader = new PdfReader(ms))
            using (var pdfDoc = new PdfDocument(pdfReader))
            {
                for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                {
                    var strategy = new SimpleTextExtractionStrategy();
                    string pageText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i), strategy);
                    text.Append(pageText);
                }
            }
            return text.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"PDF extraction failed for {entry.Name}: {ex.Message}");
            return string.Empty;
        }
    }

    private async Task<string> ReadEntryContentAsync(ZipArchiveEntry entry)
    {
        using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private bool IsTextFile(string ext)
    {
        var textExtensions = new[] { ".json", ".xml", ".html", ".htm", ".txt", ".csv", ".ttl" };
        return textExtensions.Contains(ext);
    }
}