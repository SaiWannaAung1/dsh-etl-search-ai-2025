using System.Text.Json;
using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Domain;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc; // Required for PointStruct, Value, etc.

namespace DshEtlSearch.Infrastructure.Data.VectorStore
{
    public class QdrantVectorStore : IVectorStore
    {
        private readonly QdrantClient _client;
        private readonly ILogger<QdrantVectorStore> _logger;

        public QdrantVectorStore(QdrantClient client, ILogger<QdrantVectorStore> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task CreateCollectionAsync(string collectionName, int vectorSize, CancellationToken token = default)
        {
            try
            {
                var collections = await _client.ListCollectionsAsync(token);
                if (collections.Contains(collectionName)) return;

                // Create collection with Cosine similarity
                await _client.CreateCollectionAsync(collectionName, new VectorParams
                {
                    Size = (ulong)vectorSize,
                    Distance = Distance.Cosine
                }, cancellationToken: token);

                _logger.LogInformation($"Created Qdrant collection: {collectionName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create collection {collectionName}");
            }
        }

        public async Task UpsertVectorsAsync(string collectionName, IEnumerable<EmbeddingVector> vectors, CancellationToken token = default)
        {
            var batch = vectors.ToList();
            if (!batch.Any()) return;

            var points = batch.Select(v =>
            {
                // 1. Create the PointStruct
                var point = new PointStruct
                {
                    Id = new PointId { Uuid = v.Id.ToString() },
                    Vectors = v.Vector // Implicit conversion from float[]
                };

                // 2. Add Metadata manually (Payload property is Read-Only, so we use .Add)
                point.Payload.Add("source_id", new Value { StringValue = v.SourceId.ToString() });
                point.Payload.Add("source_type", new Value { StringValue = v.SourceType.ToString() });
                point.Payload.Add("source_Authors", new Value { StringValue = JsonSerializer.Deserialize<string>(v.Authors) });
               
                
                if (!string.IsNullOrEmpty(v.TextContent))
                {
                    point.Payload.Add("text_content", new Value { StringValue = v.TextContent   });
                }

                return point;
            }).ToList();

            // 3. Send Batch
            await _client.UpsertAsync(collectionName, points, cancellationToken: token);
        }

        public async Task<List<VectorSearchResult>> SearchAsync(string collectionName, float[] queryVector, int limit = 5, CancellationToken token = default)
        {
            var results = await _client.SearchAsync(
                collectionName: collectionName,
                vector: queryVector,
                limit: (ulong)limit, 
                cancellationToken: token
            );

            return results.Select(p => new VectorSearchResult(
                sourceId: p.Payload.ContainsKey("source_id") ? Guid.Parse(p.Payload["source_id"].StringValue) : Guid.Empty,
                text: p.Payload.ContainsKey("text_content") ? p.Payload["text_content"].StringValue : string.Empty,
                score: p.Score // <--- Now we capture the actual score!
            )).ToList();
        }
        
    }
}