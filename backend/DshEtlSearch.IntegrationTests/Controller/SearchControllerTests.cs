using System.Net;
using System.Net.Http.Json;
using DshEtlSearch.Api.Models.Requests;
using DshEtlSearch.Api.Models.Responses;
using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Domain; // Added to access the Dataset entity
using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Core.Interfaces.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace DshEtlSearch.IntegrationTests.Controller;

public class SearchControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IEmbeddingService> _mockEmbedding = new();
    private readonly Mock<IVectorStore> _mockVectorStore = new();
    private readonly Mock<IMetadataRepository> _mockRepo = new();

    public SearchControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Inject our mocks into the test server's DI container
                services.AddScoped(_ => _mockEmbedding.Object);
                services.AddScoped(_ => _mockVectorStore.Object);
                services.AddScoped(_ => _mockRepo.Object);
            });
        });
    }

    [Fact]
    public async Task Search_ShouldReturnOk_WithValidResults()
    {
        // Arrange
        var client = _factory.CreateClient();
        var datasetId = Guid.NewGuid();
        var documentId = Guid.NewGuid(); // Represents the specific vector point ID
        var request = new SearchRequest { Query = "climate change", Limit = 5, MinimumScore = 0.5f };

        // 1. Mock Embedding Service
        _mockEmbedding.Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<float[]>.Success(new float[384]));

        // 2. Mock Vector Store (Qdrant) Results
        var searchHits = new List<VectorSearchResult>
        {
            // Note: We assign hit.Id so DocumentId can be populated in the response
            new VectorSearchResult(datasetId, "Chunk of text content...", 0.85f) { SourceId = documentId }
        };
        _mockVectorStore.Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchHits);

        // 3. Mock Metadata Repository
        // The controller now calls repository.GetByIdAsync to get Title, Authors, and Abstract
        var mockDataset = new Dataset("EIDC-123", "Climate Study 2024")
        {
            Authors = "Barnett, C. from UKCEH / Wells, C. from UKCEH",
            Abstract = "This is a detailed abstract about climate change that should be truncated to 50 words."
        };
        
        _mockRepo.Setup(x => x.GetByIdAsync(datasetId))
            .ReturnsAsync(mockDataset);

        // Act
        var response = await client.PostAsJsonAsync("/api/search", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<SearchResponse>>();
        
        results.Should().NotBeNull();
        results!.Should().HaveCount(1);
        
        // Assertions matching your new SearchResponse properties
        results[0].DatasetId.Should().Be(datasetId);
        results[0].DocumentId.Should().Be(documentId.ToString());
        results[0].Title.Should().Be("Climate Study 2024");
        results[0].Authors.Should().Be("Barnett, C. from UKCEH / Wells, C. from UKCEH");
        results[0].ConfidenceScore.Should().Be(0.85f);
        
        // Ensure the preview abstract is populated (and truncated if it was long)
        results[0].PreviewAbstract.Should().NotBeEmpty();
        results[0].PreviewAbstract.Should().Contain("climate change");
    }

    [Fact]
    public async Task Search_ShouldReturnBadRequest_WhenQueryIsEmpty()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new SearchRequest { Query = "" };

        // Act
        var response = await client.PostAsJsonAsync("/api/search", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}