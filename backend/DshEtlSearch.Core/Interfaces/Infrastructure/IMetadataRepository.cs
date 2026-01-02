using DshEtlSearch.Core.Domain;

namespace DshEtlSearch.Core.Interfaces.Infrastructure;

public interface IMetadataRepository
{
    // --- Standard CRUD ---
    Task<Dataset?> GetByIdAsync(Guid id);
    Task<Dataset?> GetByFileIdentifierAsync(string fileIdentifier);
    Task AddAsync(Dataset dataset);
    Task UpdateAsync(Dataset dataset);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(string fileIdentifier);

    // --- Specification Pattern Methods ---
    // These allow advanced querying (e.g., search filters) without changing the interface
    Task<List<Dataset>> ListAsync(ISpecification<Dataset> spec);
    Task<Dataset?> GetEntityWithSpec(ISpecification<Dataset> spec);
    
    // --- Transaction Management ---
    Task<int> SaveChangesAsync();
}