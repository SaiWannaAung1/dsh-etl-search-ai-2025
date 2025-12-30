using DshEtlSearch.Core.Common.Enums;

namespace DshEtlSearch.Core.Domain
{
    public class MetadataRecord
    {
        public Guid Id { get; set; }
        public Guid DatasetId { get; set; } // Foreign Key
        
        public string Title { get; set; }
        public string? Abstract { get; set; }
        public string? Authors { get; set; } // Comma separated or JSON array
        public DateTime? PublishedDate { get; set; }
        public string? Keywords { get; set; } // Comma separated
        
        public MetadataFormat SourceFormat { get; set; }
        
        // Helper to combine text for embedding generation
        public string ToEmbeddingText()
        {
            return $"Title: {Title}\nAbstract: {Abstract}\nKeywords: {Keywords}";
        }
    }
}