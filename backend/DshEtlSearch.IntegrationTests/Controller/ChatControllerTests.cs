using System.Net.Http.Json;
using DshEtlSearch.Api.Models.Requests;
using DshEtlSearch.Api.Models.Responses;
using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Core.Interfaces.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace DshEtlSearch.IntegrationTests.Controller;

public class ChatControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<ILlmService> _mockLlm = new();
    private readonly Mock<IEmbeddingService> _mockEmbedding = new();
    private readonly Mock<IVectorStore> _mockVectorStore = new();

    public ChatControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Register all mocks so the real services (Gemini, Qdrant) are NOT used
                services.AddScoped(_ => _mockLlm.Object);
                services.AddScoped(_ => _mockEmbedding.Object);
                services.AddScoped(_ => _mockVectorStore.Object);
            });
        });
    }

    [Fact]
    public async Task Ask_Endpoint_ShouldWorkWithMocks()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new ChatRequest { Message = "How much rain?", MaxContextChunks = 1 };

        // 1. Mock the Vector Generation
        _mockEmbedding.Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<float[]>.Success(new float[384]));

        // 2. Mock the Search Result (Pretend we found a Docx snippet)
        var searchHits = new List<VectorSearchResult> { 
            new VectorSearchResult(Guid.NewGuid(), "It rained 50mm in Scotland.", 0.95f) 
        };
        _mockVectorStore.Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchHits);

        // 3. Mock the Gemini Answer
        _mockLlm.Setup(x => x.GenerateAnswerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("The recorded rainfall was 50mm."));

        // Act
        var response = await client.PostAsJsonAsync("/api/chat/ask", request);

        // Assert
        response.EnsureSuccessStatusCode(); // Should be 200 now!
        var data = await response.Content.ReadFromJsonAsync<ChatResponse>();
        data!.Answer.Should().Be("The recorded rainfall was 50mm.");
    }
}