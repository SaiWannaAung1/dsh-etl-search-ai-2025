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
            _service = new ZipExtractionService(new NullLogger<ZipExtractionService>());
        }

        [Fact]
        public async Task ExtractDocumentsAsync_ShouldExtractText_IntoMemoryObject()
        {
            // Arrange
            var datasetId = Guid.NewGuid();
            var expectedContent = "Hello Integration Test Content";
            var fileName = "test-doc.txt";

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var entry = archive.CreateEntry(fileName);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                writer.Write(expectedContent);
            }
            memoryStream.Position = 0;

            // Act
            var result = await _service.ExtractDocumentsAsync(memoryStream, datasetId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Count.Should().Be(1);

            var doc = result.Value.First();
            doc.FileName.Should().Be(fileName);
            doc.DatasetId.Should().Be(datasetId);
            doc.Type.Should().Be(FileType.Unknown); 
            doc.ExtractedText.Should().Be(expectedContent);
        }

        [Fact]
        public async Task ExtractDocumentsAsync_ShouldInclude_BinaryFiles_WithContent()
        {
            // Arrange
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var entry = archive.CreateEntry("image.png");
                using var stream = entry.Open();
                
                // Write a specific byte so we can verify it was extracted
                stream.WriteByte(65); // 65 is ASCII for 'A'
            }
            memoryStream.Position = 0;

            // Act
            var result = await _service.ExtractDocumentsAsync(memoryStream, Guid.NewGuid());

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(1);
            
            var doc = result.Value!.First();
            doc.FileName.Should().Be("image.png");
            doc.Type.Should().Be(FileType.Unknown);

            // FIX: Since we download everything as text, we expect the content to be present.
            // We wrote byte 65 ('A'), so StreamReader reads it as "A".
            doc.ExtractedText.Should().Be("A"); 
        }
    }
}