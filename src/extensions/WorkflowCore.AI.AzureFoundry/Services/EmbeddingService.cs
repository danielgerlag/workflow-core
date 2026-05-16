using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.Inference;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Models;

namespace WorkflowCore.AI.AzureFoundry.Services
{
    /// <summary>
    /// Service for generating embeddings using Azure AI Inference
    /// </summary>
    public class EmbeddingService : IEmbeddingService
    {
        private readonly AzureFoundryClientFactory _clientFactory;
        private readonly AzureFoundryOptions _options;
        private readonly ILogger<EmbeddingService> _logger;

        public EmbeddingService(
            AzureFoundryClientFactory clientFactory,
            IOptions<AzureFoundryOptions> options,
            ILogger<EmbeddingService> logger)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<EmbeddingResponse> GenerateEmbeddingAsync(
            string text,
            string model = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Text cannot be null or empty", nameof(text));

            _logger.LogDebug("Generating embedding for text of length {Length}", text.Length);

            var options = new EmbeddingsOptions(new List<string> { text })
            {
                Model = model ?? _options.DefaultEmbeddingModel
            };
            var client = _clientFactory.CreateEmbeddingsClient();
            var response = await client.EmbedAsync(options, cancellationToken);
            var embedding = response.Value;

            var embeddingItem = embedding.Data.FirstOrDefault();
            float[] vector = null;
            if (embeddingItem?.Embedding != null)
            {
                var bytes = embeddingItem.Embedding.ToArray();
                vector = new float[bytes.Length / sizeof(float)];
                Buffer.BlockCopy(bytes, 0, vector, 0, bytes.Length);
            }

            return new EmbeddingResponse
            {
                Embedding = vector,
                Model = model ?? _options.DefaultEmbeddingModel,
                TokensUsed = embedding.Usage?.TotalTokens ?? 0
            };
        }
    }
}
