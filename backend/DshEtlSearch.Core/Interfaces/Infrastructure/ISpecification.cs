using System.Linq.Expressions;

namespace DshEtlSearch.Core.Interfaces;

public interface ISpecification<T>
{
    // The "Where" clause
    Expression<Func<T, bool>>? Criteria { get; }
    
    // The "Include" clauses (Joins)
    // FIX: This is the property required by your Repository
    List<Expression<Func<T, object>>> Includes { get; }
    
    // Optional: Include by string (for nested includes like "Metadata.Authors")
    List<string> IncludeStrings { get; }
    
    // Optional: Sorting
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }
}