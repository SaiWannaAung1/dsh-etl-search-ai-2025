using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DshEtlSearch.Core.Common.Enums;

namespace DshEtlSearch.Core.Domain
{
    public class MetadataRecord
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid DatasetId { get; set; }
        
        [ForeignKey(nameof(DatasetId))]
        public Dataset? Dataset { get; set; }

        // Stores "ISO19115_XML", "JsonExpanded", "RdfTurtle", etc.
        public string Format { get; set; }     
        public string RawContent { get; set; } 
        
        public MetadataRecord() { }

        public MetadataRecord(Guid datasetId, string format, string rawContent)
        {
            Id = Guid.NewGuid();
            DatasetId = datasetId;
            Format = format;
            RawContent = rawContent;
        }
    }
}