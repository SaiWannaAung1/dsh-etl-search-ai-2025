using DshEtlSearch.Core.Common;

namespace DshEtlSearch.Core.Interfaces.Application
{
    public interface IEtlOrchestrator
    {
        /// <summary>
        /// Orchestrates the full ETL pipeline: Download -> Extract -> Parse -> Save.
        /// </summary>
        /// <param name="datasetUrl">The direct URL to the dataset zip file.</param>
        /// <returns>The GUID of the newly created dataset.</returns>
        Task<Result<Guid>> ImportDatasetAsync(string datasetUrl);
    }
}