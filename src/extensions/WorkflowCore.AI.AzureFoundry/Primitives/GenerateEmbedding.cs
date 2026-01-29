using System;
using System.Threading.Tasks;
using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.AI.AzureFoundry.Primitives
{
    /// <summary>
    /// Step body for generating embeddings
    /// </summary>
    public class GenerateEmbedding : StepBodyAsync
    {
        private readonly IEmbeddingService _embeddingService;

        public GenerateEmbedding(IEmbeddingService embeddingService)
        {
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        }

        /// <summary>
        /// Text to generate embedding for
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Model to use (optional, uses default if not specified)
        /// </summary>
        public string Model { get; set; }

        // Outputs

        /// <summary>
        /// The generated embedding vector
        /// </summary>
        public float[] Embedding { get; set; }

        /// <summary>
        /// Dimensionality of the embedding
        /// </summary>
        public int Dimensions { get; set; }

        /// <summary>
        /// Tokens used for embedding
        /// </summary>
        public int TokensUsed { get; set; }

        public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
        {
            if (string.IsNullOrEmpty(Text))
            {
                throw new InvalidOperationException("Text is required for embedding generation");
            }

            var result = await _embeddingService.GenerateEmbeddingAsync(
                Text,
                Model,
                context.CancellationToken);

            Embedding = result.Embedding;
            Dimensions = result.Dimensions;
            TokensUsed = result.TokensUsed;

            return ExecutionResult.Next();
        }
    }
}
