using DshEtlSearch.Core.Common;

// CHANGE 4: Ensure this namespace matches the folder structure
namespace DshEtlSearch.Core.Interfaces.Services; 

public interface IEtlService
{
    /// <summary>
    /// Processes a single dataset: Metadata Download -> Parsing -> Zip Extraction -> DB Save.
    /// </summary>
    /// <param name="fileIdentifier">The CEH File Identifier (GUID)</param>
    Task<Result> IngestDatasetAsync(string fileIdentifier, CancellationToken token = default);

    /// <summary>
    /// Batch process: Reads 'metadata-file-identifiers.txt' and processes each ID sequentially.
    /// </summary>
    Task RunBatchIngestionAsync(CancellationToken token = default);}