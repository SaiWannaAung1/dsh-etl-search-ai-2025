using System.Linq.Expressions;

namespace DshEtlSearch.Core.Interfaces
{
    public interface ISpecification<T>
    {
        // The "Where" clause (e.g., x => x.Id == id)
        Expression<Func<T, bool>> Criteria { get; }
        
        // The "Include" strings (e.g., "Metadata", "Documents")
        List<string> IncludeStrings { get; }
    }
}