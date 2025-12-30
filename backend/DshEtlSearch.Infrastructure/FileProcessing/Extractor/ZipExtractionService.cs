using System.IO.Compression;
using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DshEtlSearch.Infrastructure.FileProcessing.Extractor
{
    public class ZipExtractionService : IExtractionService
    {
        private readonly ILogger<ZipExtractionService> _logger;

        public ZipExtractionService(ILogger<ZipExtractionService> logger)
        {
            _logger = logger;
        }

        public async Task<Result<List<string>>> ExtractZipAsync(Stream zipStream, string outputFolder)
        {
            var extractedFiles = new List<string>();

            try
            {
                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                // 'using' ensures the archive is properly closed after operation
                using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                
                foreach (var entry in archive.Entries)
                {
                    // Skip directories
                    if (string.IsNullOrEmpty(entry.Name)) continue;

                    // 1. Construct Full Path
                    string destinationPath = Path.Combine(outputFolder, entry.FullName);
                    string fullOutputFolder = Path.GetFullPath(outputFolder);

                    // 2. Security Check: Zip Slip Vulnerability
                    // Ensure the final path is actually INSIDE the output folder
                    if (!Path.GetFullPath(destinationPath).StartsWith(fullOutputFolder, StringComparison.Ordinal))
                    {
                        _logger.LogWarning("Zip Slip attempt detected: {EntryName}", entry.FullName);
                        continue; 
                    }

                    // 3. Ensure directory exists for nested files
                    var directoryName = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(directoryName))
                        Directory.CreateDirectory(directoryName);

                    // 4. Extract
                    entry.ExtractToFile(destinationPath, overwrite: true);
                    extractedFiles.Add(destinationPath);
                }

                return Result<List<string>>.Success(extractedFiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract zip archive.");
                return Result<List<string>>.Failure($"Extraction failed: {ex.Message}");
            }
        }
    }
}