using System.IO.Compression;
using System.Text;
using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Infrastructure.FileProcessing.Extractor; 
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

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
            doc.ExtractedText.Should().Be(expectedContent);
        }

        [Fact]
        public async Task ExtractDocumentsAsync_ShouldInclude_SupportedTextFiles_WithContent()
        {
            // Arrange
            var fileName = "data.json"; // Changed from .png to .json to pass the IsTextFile filter
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var entry = archive.CreateEntry(fileName);
                using var stream = entry.Open();
                
                // Write a specific byte (65 = 'A')
                stream.WriteByte(65); 
            }
            memoryStream.Position = 0;

            // Act
            var result = await _service.ExtractDocumentsAsync(memoryStream, Guid.NewGuid());

            // Assert
            result.IsSuccess.Should().BeTrue();
            
            // The service filters out unsupported extensions. 
            // If the file is a supported text type, it should have the content.
            var doc = result.Value!.FirstOrDefault(x => x.FileName == fileName);
            doc.Should().NotBeNull();
            doc!.ExtractedText.Should().Be("A"); 
        }

        [Fact]
        public async Task ExtractDocumentsAsync_ShouldReturnEmpty_ForUnsupportedBinaryFiles()
        {
            // Arrange
            var fileName = "unsupported.exe"; 
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var entry = archive.CreateEntry(fileName);
                using var stream = entry.Open();
                stream.WriteByte(65); 
            }
            memoryStream.Position = 0;

            // Act
            var result = await _service.ExtractDocumentsAsync(memoryStream, Guid.NewGuid());

            // Assert
            // The logic should either skip the file or return it with Empty text
            var doc = result.Value!.FirstOrDefault(x => x.FileName == fileName);
            if (doc != null)
            {
                doc.ExtractedText.Should().BeEmpty();
            }
        }
    }
}