using DshEtlSearch.Core.Domain;

namespace DshEtlSearch.Core.Interfaces.Infrastructure
{
    /// <summary>
    /// Defines the contract for persisting Dataset metadata and relationships.
    /// Follows the Repository Pattern to abstract data access details.
    /// </summary>
    public interface IMetadataRepository
    {
     
        // Retrieves a dataset by its unique ID, including metadata and documents.
        Task<Dataset?> GetByIdAsync(Guid id);

        // Checks if a dataset with the given source identifier (e.g., DOI) already exists.
        Task<bool> ExistsAsync(string sourceIdentifier);

        // Persists a new dataset to the database.
        Task AddAsync(Dataset dataset);
        
        // Saves all pending changes to the database context.
        Task SaveChangesAsync();
    }
}