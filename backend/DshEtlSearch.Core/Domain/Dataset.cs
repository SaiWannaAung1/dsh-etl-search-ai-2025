using DshEtlSearch.Core.Common.Enums;

namespace DshEtlSearch.Core.Domain
{
    public class Dataset
    {
        public Guid Id { get; private set; }
        public string SourceIdentifier { get; private set; } // e.g., "doi:10.5285/..."
        public DateTime IngestedAt { get; private set; }
        
        // Navigation Properties
        public MetadataRecord Metadata { get; set; }
        public List<SupportingDocument> Documents { get; private set; } = new();
        public List<EmbeddingVector> Embeddings { get; private set; } = new();

        public Dataset(string sourceIdentifier)
        {
            Id = Guid.NewGuid();
            SourceIdentifier = sourceIdentifier ?? throw new ArgumentNullException(nameof(sourceIdentifier));
            IngestedAt = DateTime.UtcNow;
        }

        // Domain Method: Link a document
        public void AddDocument(string fileName, FileType type, long sizeBytes)
        {
            Documents.Add(new SupportingDocument(Id, fileName, type, sizeBytes));
        }
    }
}