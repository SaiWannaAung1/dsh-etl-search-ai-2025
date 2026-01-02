using System.Net;
using DshEtlSearch.Infrastructure.FileProcessing.Downloader;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace DshEtlSearch.UnitTests.Infrastructure.Downloader
{
    public class CehDatasetDownloaderTests
    {
        [Fact]
        public async Task DownloadStreamAsync_ShouldReturnStream_WhenResponseIsOk()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            var content = "File Content";
            
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(content)
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var downloader = new CehDatasetDownloader(httpClient, new NullLogger<CehDatasetDownloader>());

            // Act
            var result = await downloader.DownloadStreamAsync("http://example.com/file.zip");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            
            using var reader = new StreamReader(result.Value!);
            var text = await reader.ReadToEndAsync();
            text.Should().Be(content);
        }

        [Fact]
        public async Task DownloadStreamAsync_ShouldFail_WhenResponseIsError()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var downloader = new CehDatasetDownloader(httpClient, new NullLogger<CehDatasetDownloader>());

            // Act
            var result = await downloader.DownloadStreamAsync("http://example.com/missing.zip");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("NotFound");
        }
    }
}