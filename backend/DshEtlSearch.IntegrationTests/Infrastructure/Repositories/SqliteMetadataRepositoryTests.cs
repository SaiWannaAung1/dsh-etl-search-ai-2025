using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Interfaces;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Infrastructure.Data.SQLite;
using Microsoft.EntityFrameworkCore;

namespace DshEtlSearch.IntegrationTests.Infrastructure.Repositories;

public class SqliteMetadataRepository : IMetadataRepository
{
    private readonly AppDbContext _context;

    public SqliteMetadataRepository(AppDbContext context)
    {
        _context = context;
    }

    // --- Core Entity Methods ---

    public async Task<Dataset?> GetByIdAsync(Guid id)
    {
        return await _context.Datasets
            .Include(d => d.MetadataRecords)
            .Include(d => d.SupportingDocuments)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task AddAsync(Dataset dataset)
    {
        await _context.Datasets.AddAsync(dataset);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(string fileIdentifier)
    {
        return await _context.Datasets.AnyAsync(d => d.FileIdentifier == fileIdentifier);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    // --- Specification Implementations (Datasets) ---

    public async Task<List<Dataset>> ListAsync(ISpecification<Dataset> spec)
    {
        return await ApplySpecification(spec).ToListAsync();
    }

    public async Task<Dataset?> GetEntityWithSpec(ISpecification<Dataset> spec)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync();
    }

    // --- FIX: Missing Interface Implementation (DataFiles) ---
    // This is what was causing the 'not implemented' error in your tests
    public async Task<List<DataFile>> ListFilesAsync(ISpecification<DataFile> spec)
    {
        var query = _context.SupportingDocuments.AsQueryable();

        // Apply Criteria
        if (spec.Criteria != null)
            query = query.Where(spec.Criteria);

        // Apply Includes
        if (spec.Includes != null)
            query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));

        return await query.ToListAsync();
    }

    // --- Helper for Dataset Specs ---
    private IQueryable<Dataset> ApplySpecification(ISpecification<Dataset> spec)
    {
        var query = _context.Datasets.AsQueryable();

        if (spec.Criteria != null)
            query = query.Where(spec.Criteria);

        if (spec.Includes != null)
            query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));

        return query;
    }

    // --- Other required interface members (Implement as needed for tests) ---
    public async Task<Dataset?> GetByFileIdentifierAsync(string id) => throw new NotImplementedException();
    public Task UpdateAsync(Dataset dataset) => throw new NotImplementedException();
    public Task DeleteAsync(Guid id) => throw new NotImplementedException();
}