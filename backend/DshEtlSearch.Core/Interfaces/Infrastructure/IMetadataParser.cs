using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Domain;

namespace DshEtlSearch.Core.Interfaces.Infrastructure
{
    public interface IMetadataParser
    {
        /// <summary>
        /// Parses a raw data stream into a standardized MetadataRecord.
        /// </summary>
        /// <param name="content">The file stream (XML, JSON, etc.)</param>
        /// <returns>A Result containing the parsed record or an error message.</returns>
        Task<Result<MetadataRecord>> ParseAsync(Stream content);
    }
}