using DshEtlSearch.Core.Common;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using BERTTokenizers; 

namespace DshEtlSearch.Infrastructure.ExternalServices.Ceh
{
    public class OnnxEmbeddingService : IEmbeddingService, IDisposable
    {
        private readonly InferenceSession _session;
        private readonly BertUncasedBaseTokenizer _tokenizer; 
        private readonly ILogger<OnnxEmbeddingService> _logger;

        // BGE-Small uses 384 dimensions
        public int VectorSize => 384; 

        // Constructor now finds the path automatically
        public OnnxEmbeddingService(ILogger<OnnxEmbeddingService> logger)
        {
            _logger = logger;
            try 
            {
                // 1. Locate model in the build output folder (bin/Debug/net10.0/)
                var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model.onnx");
                
                if (!File.Exists(modelPath))
                {
                    throw new FileNotFoundException($"Model not found at {modelPath}. Did you set 'Copy to Output Directory'?");
                }

                var options = new SessionOptions(); 
                _session = new InferenceSession(modelPath, options);
                
                // 2. Initialize Tokenizer (Use Base for bge-small)
                _tokenizer = new BertUncasedBaseTokenizer();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to initialize ONNX session: {ex.Message}");
                throw;
            }
        }

        public async Task<Result<float[]>> GenerateEmbeddingAsync(string text, CancellationToken token = default)
        {
            return await Task.Run(() => 
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(text)) return Result<float[]>.Failure("Empty text");

                    // A. Encode
                    var encodedList = _tokenizer.Encode(512, text);

                    // B. Extract Arrays (LINQ style)
                    var inputIds = encodedList.Select(t => t.InputIds).ToArray();
                    var tokenTypeIds = encodedList.Select(t => t.TokenTypeIds).ToArray();
                    var attentionMask = encodedList.Select(t => t.AttentionMask).ToArray();

                    // C. Create Tensors (No 'using' here)
                    var dimensions = new[] { 1, inputIds.Length };

                    var inputIdsTensor = new DenseTensor<long>(inputIds, dimensions);
                    var attentionMaskTensor = new DenseTensor<long>(attentionMask, dimensions);
                    var tokenTypeIdsTensor = new DenseTensor<long>(tokenTypeIds, dimensions);

                    var inputs = new List<NamedOnnxValue>
                    {
                        NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
                        NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor),
                        NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIdsTensor)
                    };

                    // D. Run Inference
                    using var results = _session.Run(inputs);
                    var output = results.First().AsTensor<float>();

                    // E. Post-Processing
                    var vector = MeanPooling(output, attentionMask);
                    var normalized = Normalize(vector);

                    return Result<float[]>.Success(normalized);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Embedding generation failed");
                    return Result<float[]>.Failure($"Embedding failed: {ex.Message}");
                }
            }, token);
        }

        public async Task<Result<List<float[]>>> GenerateEmbeddingsBatchAsync(List<string> texts, CancellationToken token = default)
        {
            var results = new List<float[]>();
            foreach (var text in texts)
            {
                var res = await GenerateEmbeddingAsync(text, token);
                if (res.IsSuccess) results.Add(res.Value!);
                else return Result<List<float[]>>.Failure($"Batch failed: {res.Error}");
            }
            return Result<List<float[]>>.Success(results);
        }

        private float[] MeanPooling(Tensor<float> output, long[] attentionMask)
        {
            var hiddenSize = VectorSize;
            var sequenceLength = attentionMask.Length;
            var pooled = new float[hiddenSize];
            int validCount = 0;

            for (int i = 0; i < sequenceLength; i++)
            {
                if (attentionMask[i] == 0) continue;
                validCount++;
                for (int j = 0; j < hiddenSize; j++) pooled[j] += output[0, i, j];
            }

            if (validCount > 0)
                for (int j = 0; j < hiddenSize; j++) pooled[j] /= validCount;

            return pooled;
        }

        private float[] Normalize(float[] vector)
        {
            var len = (float)Math.Sqrt(vector.Sum(x => x * x));
            return len == 0 ? vector : vector.Select(x => x / len).ToArray();
        }

        public void Dispose() => _session?.Dispose();
    }
}