using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis; 
using DshEtlSearch.Core.Common.Enums;

namespace DshEtlSearch.Core.Domain
{
    public class Dataset
    {
        [Key]
        public Guid Id { get; private set; }

        [Required]
        public string FileIdentifier { get; private set; } = null!;

        public required string Title { get; set; }
        public string? Abstract { get; set; }
        public string? Authors { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string? Keywords { get; set; }
        public string? ResourceUrl { get; set; }

        public DateTime IngestedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? LastUpdated { get; private set; }

        // --- FIX: Use a List to store multiple formats ---
        public List<MetadataRecord> MetadataRecords { get; private set; } = new();

        public List<SupportingDocument> SupportingDocuments { get; private set; } = new();
        public List<EmbeddingVector> Embeddings { get; private set; } = new();

        private Dataset() { }

        [SetsRequiredMembers]
        public Dataset(string fileIdentifier, string title)
        {
            if (string.IsNullOrWhiteSpace(fileIdentifier)) throw new ArgumentException("ID required");
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title required");

            Id = Guid.NewGuid();
            FileIdentifier = fileIdentifier;
            Title = title;
            IngestedAt = DateTime.UtcNow;
        }

        public void AddDocument(SupportingDocument doc)
        {
            SupportingDocuments.Add(doc);
            LastUpdated = DateTime.UtcNow;
        }

        // --- FIX: Add to list instead of overwriting ---
        public void AddRawMetadata(string format, string rawContent)
        {
            MetadataRecords.Add(new MetadataRecord(Id, format, rawContent));
        }
    }
}