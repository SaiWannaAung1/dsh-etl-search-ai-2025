using DshEtlSearch.Core.Common;

namespace DshEtlSearch.Core.Interfaces.Infrastructure
{
    public interface IExtractionService
    {
        /// <summary>
        /// Extracts a ZIP archive stream to a specified folder.
        /// </summary>
        /// <returns>A list of full paths to the extracted files.</returns>
        Task<Result<List<string>>> ExtractZipAsync(Stream zipStream, string outputFolder);
    }
}