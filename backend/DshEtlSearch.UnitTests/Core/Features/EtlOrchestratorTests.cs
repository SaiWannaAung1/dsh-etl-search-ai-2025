using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Common.Enums;
using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Features.Ingestion;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Core.Interfaces.Services; // Needed for IEmbeddingService
using Microsoft.Extensions.Logging; // Needed for ILogger
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
    
    // --- NEW MOCKS ---
    private readonly Mock<IEmbeddingService> _mockEmbedding;
    private readonly Mock<IVectorStore> _mockVectorStore;
    private readonly Mock<ILogger<EtlOrchestrator>> _mockLogger;
    
    private readonly EtlOrchestrator _etlService;

    public EtlOrchestratorTests()
    {
        _mockCehClient = new Mock<ICehCatalogueClient>();
        _mockArchive = new Mock<IArchiveProcessor>();
        _mockRepo = new Mock<IMetadataRepository>();
        _mockFactory = new Mock<IMetadataParserFactory>();
        _mockParser = new Mock<IMetadataParser>();
        
        // Initialize new mocks
        _mockEmbedding = new Mock<IEmbeddingService>();
        _mockVectorStore = new Mock<IVectorStore>();
        _mockLogger = new Mock<ILogger<EtlOrchestrator>>();

        _mockFactory.Setup(f => f.GetParser(It.IsAny<MetadataFormat>()))
                    .Returns(_mockParser.Object);

        // Inject all dependencies including the new ones
        _etlService = new EtlOrchestrator(
            _mockCehClient.Object,
            _mockArchive.Object,
            _mockRepo.Object,
            _mockFactory.Object,
            _mockEmbedding.Object, // Injected
            _mockVectorStore.Object, // Injected
            _mockLogger.Object // Injected
        );
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
            new SupportingDocument(Guid.NewGuid(), "readme.txt") 
            {
                ExtractedText = "This is some content inside the text file."
            }
        };
        _mockArchive.Setup(a => a.ExtractDocumentsAsync(It.IsAny<Stream>(), It.IsAny<Guid>()))
            .ReturnsAsync(Result<List<SupportingDocument>>.Success(docs));

        // --- NEW: Mock Embedding Generation ---
        // We must ensure this returns Success, or the vector logic will fail silently
        _mockEmbedding.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<float[]>.Success(new float[] { 0.1f, 0.2f, 0.3f }));

        // Act
        var result = await _etlService.IngestDatasetAsync(fileIdentifier);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify Repository Save (Dataset saved to SQL)
        _mockRepo.Verify(r => r.AddAsync(It.Is<Dataset>(d => 
            d.FileIdentifier == fileIdentifier && 
            d.Title == "Test Title" &&       
            d.MetadataRecords.Count > 0 &&  
            d.SupportingDocuments.Count == 1
        )), Times.Once);

        // Verify Vector Store Save (Vectors saved to Qdrant)
        _mockVectorStore.Verify(v => v.UpsertVectorsAsync(
            It.IsAny<string>(), 
            It.Is<IEnumerable<EmbeddingVector>>(list => list.Count() == 1), 
            It.IsAny<CancellationToken>()
        ), Times.Once);
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