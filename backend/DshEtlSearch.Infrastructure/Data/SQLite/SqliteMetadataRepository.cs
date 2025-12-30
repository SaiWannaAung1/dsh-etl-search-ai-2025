using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Interfaces;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DshEtlSearch.Infrastructure.Data.SQLite
{
	/// <summary>
    /// Implementation of IMetadataRepository using SQLite and EF Core.
    /// </summary>
    public class SqliteMetadataRepository : IMetadataRepository
    {
        private readonly AppDbContext _context;

        public SqliteMetadataRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task AddAsync(Dataset dataset)
        {
            if (dataset == null) throw new ArgumentNullException(nameof(dataset));
            await _context.Datasets.AddAsync(dataset);
        }

       
        public async Task<bool> ExistsAsync(string sourceIdentifier)
        {
            if (string.IsNullOrWhiteSpace(sourceIdentifier)) return false;
            return await _context.Datasets.AnyAsync(d => d.SourceIdentifier == sourceIdentifier);
        }

        public async Task<Dataset?> GetByIdAsync(Guid id)
        {
            return await _context.Datasets.FindAsync(id);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        // --- Specification Evaluator Logic ---

        public async Task<Dataset?> GetEntityWithSpec(ISpecification<Dataset> spec)
        {
            return await ApplySpecification(spec).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Dataset>> ListAsync(ISpecification<Dataset> spec)
        {
            return await ApplySpecification(spec).ToListAsync();
        }

        private IQueryable<Dataset> ApplySpecification(ISpecification<Dataset> spec)
        {
            // Applies the criteria (Where) and Includes (Joins) defined in the Spec
            return SpecificationEvaluator<Dataset>.GetQuery(_context.Datasets.AsQueryable(), spec);
        }
    }
}

