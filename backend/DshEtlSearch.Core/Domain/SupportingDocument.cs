using System.ComponentModel.DataAnnotations.Schema; // Required for [NotMapped]

namespace DshEtlSearch.Core.Domain
{
    public class SupportingDocument
    {
        // --- COLUMNS SAVED TO DATABASE ---
        public Guid Id { get; private set; }
        public Guid DatasetId { get; private set; }
        public string FileName { get; private set; }
        
        // --- NOT SAVED TO DATABASE (Memory Only) ---
        
        [NotMapped] 
        public string? StoragePath { get; set; } 

        [NotMapped] 
        public string ExtractedText { get; set; } = string.Empty;

        // Simplified Constructor
        public SupportingDocument(Guid datasetId, string fileName)
        {
            Id = Guid.NewGuid();
            DatasetId = datasetId;
            FileName = fileName;
        }
    }
}