using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Interfaces; // Contains ISpecification
using DshEtlSearch.Core.Interfaces.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DshEtlSearch.Infrastructure.Data.SQLite;

public class SqliteMetadataRepository : IMetadataRepository
{
    private readonly AppDbContext _context;

    public SqliteMetadataRepository(AppDbContext context)
    {
        _context = context;
    }

    // --- Standard CRUD Implementations ---

    public async Task<Dataset?> GetByIdAsync(Guid id)
    {
        return await _context.Datasets
            // FIX: Changed 'Metadata' to 'MetadataRecords'
            .Include(d => d.MetadataRecords) 
            .Include(d => d.SupportingDocuments)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Dataset?> GetByFileIdentifierAsync(string fileIdentifier)
    {
        return await _context.Datasets
            // FIX: Changed 'Metadata' to 'MetadataRecords'
            .Include(d => d.MetadataRecords)
            .Include(d => d.SupportingDocuments)
            .FirstOrDefaultAsync(d => d.FileIdentifier == fileIdentifier);
    }
    public async Task<List<DataFile>> ListFilesAsync(ISpecification<DataFile> spec)
    {
        var query = ApplyFileSpecification(spec);
        return await query.ToListAsync();
    }

    public async Task AddAsync(Dataset dataset)
    {
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Dataset dataset)
    {
        _context.Datasets.Update(dataset);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var dataset = await _context.Datasets.FindAsync(id);
        if (dataset != null)
        {
            _context.Datasets.Remove(dataset);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string fileIdentifier)
    {
        return await _context.Datasets
            .AnyAsync(d => d.FileIdentifier == fileIdentifier);
    }

    // --- Specification Implementations ---

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<List<Dataset>> ListAsync(ISpecification<Dataset> spec)
    {
        var query = ApplySpecification(spec);
        return await query.ToListAsync();
    }

    public async Task<Dataset?> GetEntityWithSpec(ISpecification<Dataset> spec)
    {
        var query = ApplySpecification(spec);
        return await query.FirstOrDefaultAsync();
    }

    // --- Helper Method ---
    private IQueryable<Dataset> ApplySpecification(ISpecification<Dataset> spec)
    {
        var query = _context.Datasets.AsQueryable();

        if (spec.Criteria != null)
        {
            query = query.Where(spec.Criteria);
        }

        if (spec.Includes != null)
        {
            query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));
        }

        return query;
    }
    
    // NEW: Helper specific to the SupportingDocuments DbSet
    private IQueryable<DataFile> ApplyFileSpecification(ISpecification<DataFile> spec)
    {
        // Use AsNoTracking() because this is a Read-Only search (better performance)
        var query = _context.SupportingDocuments.AsNoTracking().AsQueryable();

        // IMPORTANT: If spec.Criteria is null, this 'Where' is skipped, 
        // which causes the "retrieves all data" bug.
        if (spec.Criteria != null)
        {
            query = query.Where(spec.Criteria);
        }

        // Apply any eager loading (Includes)
        if (spec.Includes != null && spec.Includes.Any())
        {
            query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));
        }

        return query;
    }
}