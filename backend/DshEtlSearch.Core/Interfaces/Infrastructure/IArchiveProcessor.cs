using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Domain;

namespace DshEtlSearch.Core.Interfaces.Infrastructure;

public interface IArchiveProcessor
{
    /// <summary>
    /// Extracts supported documents (XML, PDF, etc.) from a compressed stream.
    /// </summary>
    Task<Result<List<DataFile>>> ExtractDocumentsAsync(Stream archiveStream, Guid datasetId);
}