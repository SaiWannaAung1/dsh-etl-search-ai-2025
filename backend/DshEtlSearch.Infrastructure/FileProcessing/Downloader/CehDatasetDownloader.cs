using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DshEtlSearch.Infrastructure.FileProcessing.Downloader
{
    public class CehDatasetDownloader : IDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CehDatasetDownloader> _logger;

        public CehDatasetDownloader(HttpClient httpClient, ILogger<CehDatasetDownloader> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            // Best Practice: Configure User-Agent to avoid being blocked by some servers
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DshEtlSearch/1.0");
        }

        public async Task<Result<Stream>> DownloadStreamAsync(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                    return Result<Stream>.Failure("Download URL cannot be empty.");

                _logger.LogInformation("Starting download from {Url}", url);

                // Use HttpCompletionOption.ResponseHeadersRead to avoid buffering the whole file
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                
                if (!response.IsSuccessStatusCode)
                {
                    return Result<Stream>.Failure($"Download failed with Status Code: {response.StatusCode}");
                }

                var stream = await response.Content.ReadAsStreamAsync();
                return Result<Stream>.Success(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading from {Url}", url);
                return Result<Stream>.Failure($"Download exception: {ex.Message}");
            }
        }
    }
}