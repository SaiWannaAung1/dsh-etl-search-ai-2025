using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using BERTTokenizers;
using System.Linq;

namespace DshEtlSearch.Infrastructure.ExternalServices.Ceh
{
    public class OnnxEmbeddingService : IEmbeddingService, IDisposable
    {
        private readonly InferenceSession _session;
        private readonly BertUncasedBaseTokenizer _tokenizer; 
        private readonly ILogger<OnnxEmbeddingService> _logger;

        public int VectorSize => 384; 

        public OnnxEmbeddingService(ILogger<OnnxEmbeddingService> logger)
        {
            _logger = logger;
            try 
            {
                var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model.onnx");
                
                if (!File.Exists(modelPath))
                {
                    throw new FileNotFoundException($"Model not found at {modelPath}.");
                }

                var options = new SessionOptions(); 
                // Optimization: Use all available CPU cores for big data processing
                options.IntraOpNumThreads = Environment.ProcessorCount;
                _session = new InferenceSession(modelPath, options);
                
                _tokenizer = new BertUncasedBaseTokenizer();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to initialize ONNX session: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Handles 5,000+ words by chunking and averaging embeddings.
        /// </summary>
        public async Task<Result<float[]>> GenerateEmbeddingAsync(string text, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(text)) return Result<float[]>.Failure("Empty text");

            try
            {
                // 1. Chunk long text (approx 300 words per 512 tokens)
                var chunks = ChunkText(text, maxWordsPerChunk: 300);

                // 2. Use the Batch method to process all chunks of this single document efficiently
                var batchResult = await GenerateEmbeddingsBatchAsync(chunks, token);
                if (!batchResult.IsSuccess) return Result<float[]>.Failure(batchResult.Error);

                var vectors = batchResult.Value!;

                // 3. Global Mean Pooling: Average all chunks into one document vector
                var finalVector = new float[VectorSize];
                foreach (var vec in vectors)
                {
                    for (int i = 0; i < VectorSize; i++) finalVector[i] += vec[i];
                }

                for (int i = 0; i < VectorSize; i++) finalVector[i] /= vectors.Count;

                return Result<float[]>.Success(Normalize(finalVector));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Long text embedding failed");
                return Result<float[]>.Failure(ex.Message);
            }
        }

        /// <summary>
        /// Optimized for Big Datastores: Processes multiple texts in a single ONNX pass.
        /// </summary>
    public async Task<Result<List<float[]>>> GenerateEmbeddingsBatchAsync(List<string> texts, CancellationToken token = default)
{
    return await Task.Run(() =>
    {
        try
        {
            if (texts == null || !texts.Any()) 
                return Result<List<float[]>>.Success(new List<float[]>());

            // 1. Tokenize each sentence. 
            // Result is a List of "Lists of Tokens"
            var encodedSentences = texts.Select(text => _tokenizer.Encode(512, text)).ToList();

            // 2. Explicitly specify type arguments <TSource, TResult> to fix the inference error
            // TSource: The token object returned by the library (usually 'EncodedToken' or 'BertId')
            // TResult: long (because we want a flat array of longs)
            
            var flatInputIds = encodedSentences
                .SelectMany(sentence => sentence.Select(token => token.InputIds))
                .ToArray();

            var flatTokenTypeIds = encodedSentences
                .SelectMany(sentence => sentence.Select(token => token.TokenTypeIds))
                .ToArray();

            var flatAttentionMask = encodedSentences
                .SelectMany(sentence => sentence.Select(token => token.AttentionMask))
                .ToArray();

            // 3. Define the Tensor Dimensions [BatchSize, SequenceLength]
            int batchSize = texts.Count;
            int sequenceLength = 512;
            var dimensions = new[] { batchSize, sequenceLength };

            // 4. Create Tensors
            var inputIdsTensor = new DenseTensor<long>(flatInputIds, dimensions);
            var attentionMaskTensor = new DenseTensor<long>(flatAttentionMask, dimensions);
            var tokenTypeIdsTensor = new DenseTensor<long>(flatTokenTypeIds, dimensions);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
                NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor),
                NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIdsTensor)
            };

            // 5. Run Inference and Post-Process
            using var results = _session.Run(inputs);
            var output = results.First().AsTensor<float>();

            var finalResults = new List<float[]>();
            for (int i = 0; i < batchSize; i++)
            {
                // Pull out the specific 512-length mask for this sentence in the batch
                var rowMask = flatAttentionMask.Skip(i * sequenceLength).Take(sequenceLength).ToArray();
                var vector = MeanPooling(output, rowMask, i);
                finalResults.Add(Normalize(vector));
            }

            return Result<List<float[]>>.Success(finalResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch embedding failed");
            return Result<List<float[]>>.Failure(ex.Message);
        }
    }, token);
}
        private float[] MeanPooling(Tensor<float> output, long[] attentionMask, int batchIndex = 0)
        {
            var pooled = new float[VectorSize];
            int validCount = 0;

            for (int i = 0; i < attentionMask.Length; i++)
            {
                if (attentionMask[i] == 0) continue;
                validCount++;
                for (int j = 0; j < VectorSize; j++) 
                    pooled[j] += output[batchIndex, i, j];
            }

            if (validCount > 0)
                for (int j = 0; j < VectorSize; j++) pooled[j] /= validCount;

            return pooled;
        }

        private float[] Normalize(float[] vector)
        {
            var sum = vector.Sum(x => x * x);
            var len = (float)Math.Sqrt(sum);
            return len < 1e-9f ? vector : vector.Select(x => x / len).ToArray();
        }

        private List<string> ChunkText(string text, int maxWordsPerChunk)
        {
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var chunks = new List<string>();
            for (int i = 0; i < words.Length; i += maxWordsPerChunk)
            {
                chunks.Add(string.Join(" ", words.Skip(i).Take(maxWordsPerChunk)));
            }
            return chunks;
        }

        public void Dispose() => _session?.Dispose();
    }
}