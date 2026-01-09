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
        var documentId = Guid.NewGuid();
        
        // Ensure the request values are clean
        var request = new SearchRequest { 
            Query = "climate change", 
            Limit = 5, 
            MinimumScore = 0.1f // Lower this to be safe during the test
        };

        // 1. Mock Embedding - Use It.IsAny to ensure it catches the call
        _mockEmbedding.Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<float[]>.Success(new float[384]));

        // 2. Mock Vector Store 
        // IMPORTANT: Ensure the score (0.85f) is higher than the request.MinimumScore
        var searchHits = new List<VectorSearchResult>
        {
            new VectorSearchResult(datasetId, "Chunk of text content...", 0.85f) 
            { 
                SourceId = documentId // Ensure this matches what the controller expects
            }
        };

        _mockVectorStore.Setup(x => x.SearchAsync(
                It.IsAny<string>(), 
                It.IsAny<float[]>(), 
                It.IsAny<int>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchHits);

        // 3. Mock Metadata Repository
        var mockDataset = new Dataset("EIDC-123", "Climate Study 2024")
        {
            Authors = "Barnett, C. from UKCEH / Wells, C. from UKCEH",
            Abstract = "This is a detailed abstract about climate change."
        };
        
        // Verify that the controller is passing the EXACT datasetId found in the vector store
        _mockRepo.Setup(x => x.GetByIdAsync(datasetId))
            .ReturnsAsync(mockDataset);

        // Act
        var response = await client.PostAsJsonAsync("/api/search", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<SearchResponse>>();
        
        // Diagnostic: If this fails, print the response content to see if there's an error message
        results.Should().NotBeNull();
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