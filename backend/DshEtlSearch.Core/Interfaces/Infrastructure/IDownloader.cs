using DshEtlSearch.Core.Common;

namespace DshEtlSearch.Core.Interfaces.Infrastructure
{
    public interface IDownloader
    {
        /// <summary>
        /// Downloads a file from a URL as a stream.
        /// Useful for processing large files without loading them fully into memory.
        /// </summary>
        Task<Result<Stream>> DownloadStreamAsync(string url);
    }
}