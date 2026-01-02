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

        // Setup the Factory to return our Mock Parser
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
        
        // 1. Mock Repo: Dataset does NOT exist yet
        _mockRepo.Setup(r => r.ExistsAsync(fileIdentifier)).ReturnsAsync(false);

        // 2. Mock Metadata Download: Returns a valid stream
        // We use a non-empty stream so StreamReader doesn't complain
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("<xml>content</xml>"));
        _mockCehClient.Setup(c => c.GetMetadataAsync(fileIdentifier, MetadataFormat.Iso19115Xml))
            .ReturnsAsync(Result<Stream>.Success(stream));

        // 3. Mock Parser: Returns the DTO (Not the Entity!)
        var parsedDto = new ParsedMetadataDto
        {
            Title = "Test Title",
            Abstract = "Test Abstract",
            ResourceUrl = "http://resource.url"
        };

        // FIX: Remove the 'Guid' parameter. The new Parser interface is Parse(Stream).
        _mockParser.Setup(p => p.Parse(It.IsAny<Stream>()))
            .Returns(Result<ParsedMetadataDto>.Success(parsedDto));

        // 4. Mock Zip Download: Returns a valid stream
        _mockCehClient.Setup(c => c.DownloadDatasetZipAsync(fileIdentifier))
            .ReturnsAsync(Result<Stream>.Success(new MemoryStream()));

        // 5. Mock Archive Extraction: Returns 1 supporting document
        // Note: SupportingDocument constructor requires (datasetId, filename, type, size)
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

        // Verify Repository was called to save
        // FIX: We verify that 'd.Title' is set directly on the Dataset, NOT on 'd.Metadata'
        _mockRepo.Verify(r => r.AddAsync(It.Is<Dataset>(d => 
            d.FileIdentifier == fileIdentifier && 
            d.Title == "Test Title" &&       // Checked directly on Dataset
            d.Abstract == "Test Abstract" && // Checked directly on Dataset
            d.MetadataRecords != null &&            // Metadata backup record exists
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
        Assert.Contains("Metadata download failed", result.Error);

        // Verify NO save occurred
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Dataset>()), Times.Never);
        // Verify we didn't try to download zip
        _mockCehClient.Verify(c => c.DownloadDatasetZipAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task IngestDatasetAsync_ShouldSkip_WhenDatasetAlreadyExists()
    {
        // Arrange
        string fileIdentifier = "ceh-exists";
        
        // Mock Repo: Dataset EXISTS
        _mockRepo.Setup(r => r.ExistsAsync(fileIdentifier)).ReturnsAsync(true);

        // Act
        var result = await _etlService.IngestDatasetAsync(fileIdentifier);

        // Assert
        Assert.True(result.IsSuccess); // Success because skipping is a valid outcome

        // Verify we stopped early
        _mockCehClient.Verify(c => c.GetMetadataAsync(It.IsAny<string>(), It.IsAny<MetadataFormat>()), Times.Never);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Dataset>()), Times.Never);
    }
}