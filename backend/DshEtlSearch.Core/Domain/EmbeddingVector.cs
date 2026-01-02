namespace DshEtlSearch.Core.Domain
{
    public class EmbeddingVector
    {
        public Guid Id { get; set; }
        public Guid DatasetId { get; set; }
        
        // The actual vector (e.g., 1536 dims for OpenAI)
        // Stored as JSON string or float array depending on DB choice
        public float[] VectorData { get; set; } 
        public int Dimensions { get; set; }
        
        public string ChunkText { get; set; } // The actual text segment this vector represents

        public EmbeddingVector(Guid datasetId, float[] vectorData, string chunkText)
        {
            Id = Guid.NewGuid();
            DatasetId = datasetId;
            VectorData = vectorData;
            Dimensions = vectorData.Length;
            ChunkText = chunkText;
        }
    }
}