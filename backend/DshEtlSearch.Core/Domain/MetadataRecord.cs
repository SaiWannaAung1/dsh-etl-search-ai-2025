using DshEtlSearch.Core.Common.Enums;

namespace DshEtlSearch.Core.Domain
{
    public class MetadataRecord
    {
        public Guid Id { get; set; }
        public Guid DatasetId { get; set; }
        
        // This ensures the compiler enforces initialization when creating the object.
        public required string Title { get; set; } 
        
        public string? Abstract { get; set; }
        public string? Authors { get; set; } 
        public DateTime? PublishedDate { get; set; }
        public string? Keywords { get; set; } 
        
        public MetadataFormat SourceFormat { get; set; }
        
        public string ToEmbeddingText()
        {
            return $"Title: {Title}\nAbstract: {Abstract ?? ""}\nKeywords: {Keywords ?? ""}";
        }
    }
}