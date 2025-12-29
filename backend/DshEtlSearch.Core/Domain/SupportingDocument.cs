using DshEtlSearch.Core.Common.Enums;

namespace DshEtlSearch.Core.Domain
{
    public class SupportingDocument
    {
        public Guid Id { get; private set; }
        public Guid DatasetId { get; private set; }
        
        public string FileName { get; private set; }
        public FileType Type { get; private set; }
        public long SizeBytes { get; private set; }
        public string StoragePath { get; set; } // Path in blob storage/disk

        public SupportingDocument(Guid datasetId, string fileName, FileType type, long sizeBytes)
        {
            Id = Guid.NewGuid();
            DatasetId = datasetId;
            FileName = fileName;
            Type = type;
            SizeBytes = sizeBytes;
        }
    }
}