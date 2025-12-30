using DshEtlSearch.Core.Domain;
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
            
            return await _context.Datasets
                .AnyAsync(d => d.SourceIdentifier == sourceIdentifier);
        }

        public async Task<Dataset?> GetByIdAsync(Guid id)
        {
            // Eagerly load related data (Metadata and Documents)
            return await _context.Datasets
                .Include(d => d.Metadata)
                .Include(d => d.Documents)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}