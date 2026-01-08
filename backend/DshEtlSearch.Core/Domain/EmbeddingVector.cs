using DshEtlSearch.Core.Common.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace DshEtlSearch.Core.Domain
{
    // This is NOT mapped to SQLite. It lives in Qdrant.
    [NotMapped]
    public class EmbeddingVector
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid SourceId { get; set; } // Link to Dataset or SupportingDocument
        public VectorSourceType SourceType { get; set; } 
        
        public string TextContent { get; set; } = string.Empty;
        public float[] Vector { get; set; } = Array.Empty<float>();

        // --- Metadata for Search Enrichment ---
        public string Title { get; set; } = string.Empty;
        public string? Abstract { get; set; }
        public string? Authors { get; set; }
        public string? Keywords { get; set; }

        public EmbeddingVector() { }

        // Updated constructor to accept all parameters from your call
        public EmbeddingVector(
            Guid sourceId, 
            VectorSourceType type, 
            string text, 
            float[] vector,
            string title,
            string? @abstract,
            string? authors,
            string? keywords)
        {
            Id = Guid.NewGuid();
            SourceId = sourceId;
            SourceType = type;
            TextContent = text;
            Vector = vector;
            Title = title;
            Abstract = @abstract;
            Authors = authors;
            Keywords = keywords;
        }
    }
}