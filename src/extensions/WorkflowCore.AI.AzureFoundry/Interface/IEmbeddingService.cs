using System.Threading;
using System.Threading.Tasks;

namespace WorkflowCore.AI.AzureFoundry.Interface
{
    /// <summary>
    /// Service for generating embeddings
    /// </summary>
    public interface IEmbeddingService
    {
        /// <summary>
        /// Generate an embedding vector for the given text
        /// </summary>
        /// <param name="text">Text to embed</param>
        /// <param name="model">Model to use (null for default)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Embedding vector</returns>
        Task<EmbeddingResponse> GenerateEmbeddingAsync(
            string text,
            string model = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Response from an embedding request
    /// </summary>
    public class EmbeddingResponse
    {
        /// <summary>
        /// The embedding vector
        /// </summary>
        public float[] Embedding { get; set; }

        /// <summary>
        /// Dimensionality of the embedding
        /// </summary>
        public int Dimensions => Embedding?.Length ?? 0;

        /// <summary>
        /// Model used to generate the embedding
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Tokens used
        /// </summary>
        public int TokensUsed { get; set; }
    }
}
