using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using DshEtlSearch.Infrastructure.ExternalServices.Ingestion;
using Xunit;

namespace DshEtlSearch.IntegrationTests.ExternalServices;

public class EtlOrchestratorTests
{
    private readonly Mock<ICehCatalogueClient> _mockCehClient;
    private readonly Mock<IArchiveProcessor> _mockArchive;
    private readonly Mock<IMetadataRepository> _mockRepo;
    private readonly Mock<IMetadataParserFactory> _mockFactory;
    private readonly Mock<IMetadataParser> _mockParser;
    private readonly Mock<IEmbeddingService> _mockEmbedding;
    private readonly Mock<IVectorStore> _mockVectorStore;
    private readonly Mock<ILogger<EtlOrchestrator>> _mockLogger;
    private readonly Mock<IGoogleDriveService> _mockGoogleDrive; // NEW

    private readonly EtlOrchestrator _etlService;

    public EtlOrchestratorTests()
    {
        _mockCehClient = new Mock<ICehCatalogueClient>();
        _mockArchive = new Mock<IArchiveProcessor>();
        _mockRepo = new Mock<IMetadataRepository>();
        _mockFactory = new Mock<IMetadataParserFactory>();
        _mockParser = new Mock<IMetadataParser>();
        _mockEmbedding = new Mock<IEmbeddingService>();
        _mockVectorStore = new Mock<IVectorStore>();
        _mockLogger = new Mock<ILogger<EtlOrchestrator>>();
        _mockGoogleDrive = new Mock<IGoogleDriveService>(); // Initialize Mock

        _mockFactory.Setup(f => f.GetParser(It.IsAny<MetadataFormat>()))
                    .Returns(_mockParser.Object);

        // Inject all mocks including Google Drive
        _etlService = new EtlOrchestrator(
            _mockCehClient.Object,
            _mockArchive.Object,
            _mockRepo.Object,
            _mockFactory.Object,
            _mockEmbedding.Object,
            _mockVectorStore.Object,
            _mockLogger.Object,
            _mockGoogleDrive.Object
        );
    }

  [Fact]
public async Task IngestDatasetAsync_ShouldSaveDataset_WhenFlowIsSuccessful()
{
    // Arrange
    string fileIdentifier = "ceh-12345";
    _mockRepo.Setup(r => r.ExistsAsync(fileIdentifier)).ReturnsAsync(false);

    // 1. Mock XML Metadata Download (Primary Format)
    var xmlStream = new MemoryStream(Encoding.UTF8.GetBytes("<xml>content</xml>"));
    _mockCehClient.Setup(c => c.GetMetadataAsync(fileIdentifier, MetadataFormat.Iso19115Xml))
        .ReturnsAsync(Result<Stream>.Success(xmlStream));

    // Fix: Authors as a simple string to match your DTO's string? type
    var parsedDto = new ParsedMetadataDto 
    { 
        Title = "Test Title", 
        Abstract = "Test Abstract", 
        Authors = "Author 1, Author 2",
        ResourceUrl = "https://ceh.ac.uk/data" 
    };
    
    _mockParser.Setup(p => p.Parse(It.IsAny<Stream>()))
        .Returns(Result<ParsedMetadataDto>.Success(parsedDto));

    // 2. Mock Zip Download & Extraction
    _mockCehClient.Setup(c => c.DownloadDatasetZipAsync(fileIdentifier))
        .ReturnsAsync(Result<Stream>.Success(new MemoryStream()));

    var extractedFiles = new List<DataFile> 
    { 
        // We use a path that DOES NOT contain "data/" to skip the Google Drive logic loop,
        // OR we rely on the Orchestrator's null check for _googleDriveService.
        new DataFile(Guid.NewGuid(), "readme.txt") 
        { 
            ExtractedText = "General research information" 
        } 
    };
    
    _mockArchive.Setup(a => a.ExtractDocumentsAsync(It.IsAny<Stream>(), It.IsAny<Guid>()))
        .ReturnsAsync(Result<List<DataFile>>.Success(extractedFiles));

    // 3. Mock Supporting Docs Download (Required to prevent NullRef in the second loop)
    _mockCehClient.Setup(c => c.DownloadSupportingDocsAsync(fileIdentifier))
        .ReturnsAsync(Result<Stream>.Success(new MemoryStream()));

    // 4. Mock Vector/Embedding
    _mockEmbedding.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result<float[]>.Success(new float[384]));

    // NOTE: We do not setup _mockGoogleDrive here because we are skipping it.
    // Ensure the EtlOrchestrator constructor in the [Fact] setup (or Constructor) 
    // received 'null' or the mock object for IGoogleDriveService.

    // Act
    var result = await _etlService.IngestDatasetAsync(fileIdentifier);

    // Assert
    Assert.True(result.IsSuccess);
    
    // Verify that the Dataset was saved with the expected Title
    _mockRepo.Verify(r => r.AddAsync(It.Is<Dataset>(d => 
        d.Title == "Test Title" && 
        d.FileIdentifier == fileIdentifier
    )), Times.Once);
}

    [Fact]
    public async Task IngestDatasetAsync_ShouldFail_WhenMetadataDownloadFails()
    {
        // Arrange
        string fileIdentifier = "ceh-fail";
        _mockRepo.Setup(r => r.ExistsAsync(fileIdentifier)).ReturnsAsync(false);
        _mockCehClient.Setup(c => c.GetMetadataAsync(fileIdentifier, MetadataFormat.Iso19115Xml))
            .ReturnsAsync(Result<Stream>.Failure("404"));

        // Act
        var result = await _etlService.IngestDatasetAsync(fileIdentifier);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Primary metadata download failed", result.Error);
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
    }
}