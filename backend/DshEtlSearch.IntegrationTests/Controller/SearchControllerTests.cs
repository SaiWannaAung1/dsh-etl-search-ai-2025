using System.Net;
using System.Net.Http.Json;
using DshEtlSearch.Api.Models.Requests;
using DshEtlSearch.Api.Models.Responses;
using DshEtlSearch.Core.Common;
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
        // Setup the test server and inject our mocks instead of real services
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
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
        var request = new SearchRequest { Query = "climate change", Limit = 5, MinimumScore = 0.5f };

        // Mock Embedding Service
        _mockEmbedding.Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), default))
            .ReturnsAsync(Result<float[]>.Success(new float[384]));

        // Mock Vector Store Results (Matching the snippet extracted from DOCX/PDF)
        var searchHits = new List<VectorSearchResult>
        {
            new VectorSearchResult(datasetId, "This is a snippet about climate change.", 0.85f)
        };
        _mockVectorStore.Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<int>(), default))
            .ReturnsAsync(searchHits);

        // Act
        var response = await client.PostAsJsonAsync("/api/search", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<SearchResponse>>();
        
        results.Should().NotBeNull();
        results!.Should().HaveCount(1);
        results[0].Snippet.Should().Be("This is a snippet about climate change.");
        results[0].ConfidenceScore.Should().Be(0.85f);
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