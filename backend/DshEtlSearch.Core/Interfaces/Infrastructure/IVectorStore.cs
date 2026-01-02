using DshEtlSearch.Core.Domain;

namespace DshEtlSearch.Core.Interfaces.Infrastructure
{
    public interface IVectorStore
    {
        Task CreateCollectionAsync(string collectionName, int vectorSize, CancellationToken token = default);
        Task UpsertVectorsAsync(string collectionName, IEnumerable<EmbeddingVector> vectors, CancellationToken token = default);
        Task<List<EmbeddingVector>> SearchAsync(string collectionName, float[] queryVector, int limit = 5, CancellationToken token = default);
    }
}