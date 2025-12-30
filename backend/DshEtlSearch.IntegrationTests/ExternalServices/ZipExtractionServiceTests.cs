using System.IO.Compression;
using DshEtlSearch.Infrastructure.FileProcessing.Extractor;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DshEtlSearch.IntegrationTests.ExternalServices
{
    public class ZipExtractionServiceTests : IDisposable
    {
        private readonly string _testFolder;
        private readonly ZipExtractionService _service;

        public ZipExtractionServiceTests()
        {
            _testFolder = Path.Combine(Path.GetTempPath(), "DshIntegrationTests_" + Guid.NewGuid());
            _service = new ZipExtractionService(new NullLogger<ZipExtractionService>());
        }

        [Fact]
        public async Task ExtractZipAsync_ShouldExtractFiles_ToDisk()
        {
            // Arrange: Create a real ZIP in memory
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var file = archive.CreateEntry("test.txt");
                using var entryStream = file.Open();
                using var writer = new StreamWriter(entryStream);
                writer.Write("Hello Integration Test");
            }
            memoryStream.Position = 0; // Reset stream for reading

            // Act
            var result = await _service.ExtractZipAsync(memoryStream, _testFolder);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(1);
            File.Exists(Path.Combine(_testFolder, "test.txt")).Should().BeTrue();
            File.ReadAllText(Path.Combine(_testFolder, "test.txt")).Should().Be("Hello Integration Test");
        }

        public void Dispose()
        {
            if (Directory.Exists(_testFolder))
                Directory.Delete(_testFolder, true);
        }
    }
}