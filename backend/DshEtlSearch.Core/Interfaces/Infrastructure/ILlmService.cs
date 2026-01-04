using DshEtlSearch.Core.Common;

namespace DshEtlSearch.Core.Interfaces.Infrastructure;

public interface ILlmService
{
    Task<Result<string>> GenerateAnswerAsync(string query, string context, CancellationToken token = default);
}