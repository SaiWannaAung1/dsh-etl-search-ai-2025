using DshEtlSearch.Core.Common;

namespace DshEtlSearch.Core.Interfaces.Services 
{
    public interface IEmbeddingService
    {
        int VectorSize { get; }
        Task<Result<float[]>> GenerateEmbeddingAsync(string text, CancellationToken token = default);
       
        
        Task<Result<List<float[]>>> GenerateEmbeddingsBatchAsync(List<string> texts, CancellationToken token = default);
    }
}