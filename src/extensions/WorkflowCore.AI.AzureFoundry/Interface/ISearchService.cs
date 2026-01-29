using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.AI.AzureFoundry.Models;

namespace WorkflowCore.AI.AzureFoundry.Interface
{
    /// <summary>
    /// Service for vector search operations
    /// </summary>
    public interface ISearchService
    {
        /// <summary>
        /// Search for documents using a text query (will be embedded automatically)
        /// </summary>
        /// <param name="indexName">Name of the search index</param>
        /// <param name="query">Text query</param>
        /// <param name="topK">Number of results to return</param>
        /// <param name="filter">Optional OData filter expression</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<SearchResults> SearchAsync(
            string indexName,
            string query,
            int topK = 5,
            string filter = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Search for documents using a pre-computed embedding vector
        /// </summary>
        /// <param name="indexName">Name of the search index</param>
        /// <param name="embedding">Embedding vector</param>
        /// <param name="topK">Number of results to return</param>
        /// <param name="filter">Optional OData filter expression</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<SearchResults> SearchByVectorAsync(
            string indexName,
            float[] embedding,
            int topK = 5,
            string filter = null,
            CancellationToken cancellationToken = default);
    }
}
