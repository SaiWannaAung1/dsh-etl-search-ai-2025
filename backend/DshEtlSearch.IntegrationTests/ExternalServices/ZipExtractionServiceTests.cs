using System.IO.Compression;
using System.Text;
using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Infrastructure.FileProcessing.Extractor; 
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace DshEtlSearch.IntegrationTests.ExternalServices
{
    public class ZipExtractionServiceTests
    {
        private readonly ZipExtractionService _service;

        public ZipExtractionServiceTests()
        {
            // We use NullLogger because we don't need real logging output for this test
            _service = new ZipExtractionService(new NullLogger<ZipExtractionService>());
        }

        [Fact]
        public async Task ExtractDocumentsAsync_ShouldExtractText_IntoMemoryObject()
        {
            // Arrange
            var datasetId = Guid.NewGuid();
            var expectedContent = "Hello Integration Test Content";
            var fileName = "test-doc.txt";

            // Create a valid Zip archive in memory containing one text file
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var entry = archive.CreateEntry(fileName);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                writer.Write(expectedContent);
            }
            memoryStream.Position = 0; // Reset stream to beginning so the service can read it

            // Act
            // We pass the memory stream and a fake dataset ID
            var result = await _service.ExtractDocumentsAsync(memoryStream, datasetId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Count.Should().Be(1);

            var doc = result.Value.First();
            
            // Verify Metadata
            doc.FileName.Should().Be(fileName);
            doc.DatasetId.Should().Be(datasetId);
            doc.Type.Should().Be(FileType.Txt);
            
            // Verify Content (The critical part: ensure text was read into memory)
            doc.ExtractedText.Should().Be(expectedContent);
        }

        [Fact]
        public async Task ExtractDocumentsAsync_ShouldSkip_UnsupportedFileTypes()
        {
            // Arrange
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                // Create an image file (should be ignored based on your _supportedExtensions list)
                var entry = archive.CreateEntry("image.png");
                using var stream = entry.Open();
                stream.WriteByte(0); // Write a single byte so it's not empty
            }
            memoryStream.Position = 0;

            // Act
            var result = await _service.ExtractDocumentsAsync(memoryStream, Guid.NewGuid());

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEmpty(); // Should return an empty list, not failure
        }
    }
}