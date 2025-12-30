using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Interfaces.Application;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Infrastructure.FileProcessing.Parsers;
using DshEtlSearch.Infrastructure.Services; // Ensure this points to where EtlOrchestrator is
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace DshEtlSearch.UnitTests.Core.Features
{
    public class EtlOrchestratorTests
    {
        private readonly Mock<IDownloader> _downloaderMock;
        private readonly Mock<IExtractionService> _extractorMock;
        private readonly Mock<IMetadataRepository> _repoMock;
        private readonly MetadataParserFactory _factory; // Use real factory, it's stateless logic
        private readonly EtlOrchestrator _orchestrator;

        public EtlOrchestratorTests()
        {
            _downloaderMock = new Mock<IDownloader>();
            _extractorMock = new Mock<IExtractionService>();
            _repoMock = new Mock<IMetadataRepository>();
            _factory = new MetadataParserFactory();

            _orchestrator = new EtlOrchestrator(
                _downloaderMock.Object,
                _extractorMock.Object,
                _factory,
                _repoMock.Object,
                new NullLogger<EtlOrchestrator>()
            );
        }

        [Fact]
        public async Task ImportDatasetAsync_ShouldCompleteEtlFlow_WhenAllStepsSucceed()
        {
            // Arrange
            string url = "http://test.com/data.zip";
            
            // 1. Mock Download
            var dummyZipStream = new MemoryStream();
            _downloaderMock.Setup(d => d.DownloadStreamAsync(url))
                .ReturnsAsync(Result<Stream>.Success(dummyZipStream));

            // 2. Mock Extraction (Return a fake XML file path)
            // We create a real temp file so the Orchestrator can 'File.OpenRead' it
            var tempFile = Path.GetTempFileName() + ".xml";
            File.WriteAllText(tempFile, "<root>Valid XML</root>"); // Minimal valid XML content

            _extractorMock.Setup(e => e.ExtractZipAsync(It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync(Result<List<string>>.Success(new List<string> { tempFile }));

            // 3. Mock Parser (Since the Factory creates real parsers, we are testing Integration of Factory+Parser here implicitly, 
            //    or we could mock the IMetadataParser. However, EtlOrchestrator uses the Factory directly.
            //    To make this purely a UNIT test, we'd rely on the real parser failing on dummy XML unless we provide valid ISO XML.
            //    Let's provide valid ISO XML to make the Real Parser happy.)
            
            var validIsoXml = @"<?xml version='1.0'?><gmd:MD_Metadata xmlns:gmd='http://www.isotc211.org/2005/gmd' xmlns:gco='http://www.isotc211.org/2005/gco'><gmd:identificationInfo><gmd:title><gco:CharacterString>Success Title</gco:CharacterString></gmd:title></gmd:identificationInfo></gmd:MD_Metadata>";
            File.WriteAllText(tempFile, validIsoXml);

            // Act
            var result = await _orchestrator.ImportDatasetAsync(url);

            // Cleanup
            if (File.Exists(tempFile)) File.Delete(tempFile);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeEmpty();

            // Verify Repo was called to save
            _repoMock.Verify(r => r.AddAsync(It.Is<Dataset>(d => d.Metadata!.Title == "Success Title")), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ImportDatasetAsync_ShouldFail_WhenDownloadFails()
        {
            // Arrange
            _downloaderMock.Setup(d => d.DownloadStreamAsync(It.IsAny<string>()))
                .ReturnsAsync(Result<Stream>.Failure("404 Not Found"));

            // Act
            var result = await _orchestrator.ImportDatasetAsync("http://bad-url.com");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("404 Not Found");
            _repoMock.Verify(r => r.AddAsync(It.IsAny<Dataset>()), Times.Never);
        }
    }
}