using DshEtlSearch.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DshEtlSearch.Infrastructure.Data
{
    public class SpecificationEvaluator<T> where T : class
    {
        public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T> specification)
        {
            var query = inputQuery;

            // 1. Apply Filtering (Where)
            if (specification.Criteria != null)
            {
                query = query.Where(specification.Criteria);
            }

            // 2. Apply Includes (Joins)
            // Aggregate is a fancy LINQ way to loop and apply .Include()
            query = specification.IncludeStrings.Aggregate(query,
                (current, include) => current.Include(include));

            return query;
        }
    }
}