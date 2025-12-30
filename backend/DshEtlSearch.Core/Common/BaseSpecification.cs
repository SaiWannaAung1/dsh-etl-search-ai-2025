using System.Linq.Expressions;
using DshEtlSearch.Core.Interfaces;

namespace DshEtlSearch.Core.Common
{
    public abstract class BaseSpecification<T> : ISpecification<T>
    {
        public Expression<Func<T, bool>> Criteria { get; }
        public List<string> IncludeStrings { get; } = new List<string>();

        protected BaseSpecification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
        }

        // Helper to add "Include" statements easily
        protected void AddInclude(string includeString)
        {
            IncludeStrings.Add(includeString);
        }
    }
}