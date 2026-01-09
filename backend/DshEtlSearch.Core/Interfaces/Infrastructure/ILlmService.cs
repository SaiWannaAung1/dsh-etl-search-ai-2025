using DshEtlSearch.Core.Common;

namespace DshEtlSearch.Core.Interfaces.Infrastructure;

public interface ILlmService
{
    Task<Result<string>> GenerateAnswerAsync(string query, string context, CancellationToken token = default);
    
    Task<Result<string>> GenerateAnswerAsync(
        string query, 
        List<ChatMessage> history, // Change this from string to List
        string context, 
        CancellationToken token = default
    );
}