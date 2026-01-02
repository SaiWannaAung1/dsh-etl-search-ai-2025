using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Features.Ingestion;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using Moq;
using Xunit;

namespace DshEtlSearch.Tests.Unit.Core.Features;

public class EtlOrchestratorTests
{
    private readonly Mock<ICehCatalogueClient> _mockCehClient;
    private readonly Mock<IArchiveProcessor> _mockArchive;
    private readonly Mock<IMetadataRepository> _mockRepo;
    private readonly Mock<IMetadataParserFactory> _mockFactory;
    private readonly Mock<IMetadataParser> _mockParser;
    private readonly EtlOrchestrator _etlService;

    public EtlOrchestratorTests()
    {
        _mockCehClient = new Mock<ICehCatalogueClient>();
        _mockArchive = new Mock<IArchiveProcessor>();
        _mockRepo = new Mock<IMetadataRepository>();
        _mockFactory = new Mock<IMetadataParserFactory>();
        _mockParser = new Mock<IMetadataParser>();

        _mockFactory.Setup(f => f.GetParser(It.IsAny<MetadataFormat>()))
                    .Returns(_mockParser.Object);

        _etlService = new EtlOrchestrator(
            _mockCehClient.Object,
            _mockArchive.Object,
            _mockRepo.Object,
            _mockFactory.Object);
    }

    [Fact]
    public async Task IngestDatasetAsync_ShouldSaveDataset_WhenFlowIsSuccessful()
    {
        // Arrange
        string fileIdentifier = "ceh-12345";
        
        _mockRepo.Setup(r => r.ExistsAsync(fileIdentifier)).ReturnsAsync(false);

        // Mock Metadata Download (Success)
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("<xml>content</xml>"));
        _mockCehClient.Setup(c => c.GetMetadataAsync(fileIdentifier, MetadataFormat.Iso19115Xml))
            .ReturnsAsync(Result<Stream>.Success(stream));

        // Mock Parser (Returns DTO)
        var parsedDto = new ParsedMetadataDto
        {
            Title = "Test Title",
            Abstract = "Test Abstract",
            ResourceUrl = "http://resource.url"
        };
        _mockParser.Setup(p => p.Parse(It.IsAny<Stream>()))
            .Returns(Result<ParsedMetadataDto>.Success(parsedDto));

        // Mock Zip Download (Success)
        _mockCehClient.Setup(c => c.DownloadDatasetZipAsync(fileIdentifier))
            .ReturnsAsync(Result<Stream>.Success(new MemoryStream()));

        // Mock Archive Extraction (Success)
        var docs = new List<SupportingDocument> 
        { 
            new SupportingDocument(Guid.NewGuid(), "readme.txt", FileType.Txt, 1024) 
        };
        _mockArchive.Setup(a => a.ExtractDocumentsAsync(It.IsAny<Stream>(), It.IsAny<Guid>()))
            .ReturnsAsync(Result<List<SupportingDocument>>.Success(docs));

        // Act
        var result = await _etlService.IngestDatasetAsync(fileIdentifier);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify Repository Save
        _mockRepo.Verify(r => r.AddAsync(It.Is<Dataset>(d => 
            d.FileIdentifier == fileIdentifier && 
            d.Title == "Test Title" &&       
            d.MetadataRecords.Count > 0 &&  // Ensure metadata was added to the list
            d.SupportingDocuments.Count == 1
        )), Times.Once);
    }

    [Fact]
    public async Task IngestDatasetAsync_ShouldFail_WhenMetadataDownloadFails()
    {
        // Arrange
        string fileIdentifier = "ceh-fail";
        _mockRepo.Setup(r => r.ExistsAsync(fileIdentifier)).ReturnsAsync(false);

        // Mock Failure
        _mockCehClient.Setup(c => c.GetMetadataAsync(fileIdentifier, MetadataFormat.Iso19115Xml))
            .ReturnsAsync(Result<Stream>.Failure("404 Not Found"));

        // Act
        var result = await _etlService.IngestDatasetAsync(fileIdentifier);

        // Assert
        Assert.False(result.IsSuccess);
        
        // FIX: Updated the expected string to match the new Orchestrator logic
        Assert.Contains("Primary metadata download failed", result.Error);

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Dataset>()), Times.Never);
    }

    [Fact]
    public async Task IngestDatasetAsync_ShouldSkip_WhenDatasetAlreadyExists()
    {
        // Arrange
        string fileIdentifier = "ceh-exists";
        _mockRepo.Setup(r => r.ExistsAsync(fileIdentifier)).ReturnsAsync(true);

        // Act
        var result = await _etlService.IngestDatasetAsync(fileIdentifier);

        // Assert
        Assert.True(result.IsSuccess); 

        _mockCehClient.Verify(c => c.GetMetadataAsync(It.IsAny<string>(), It.IsAny<MetadataFormat>()), Times.Never);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Dataset>()), Times.Never);
    }
}