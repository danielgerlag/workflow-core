using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Models;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.AI.AzureFoundry.Primitives
{
    /// <summary>
    /// Step body for vector search operations
    /// </summary>
    public class VectorSearch : StepBodyAsync
    {
        private readonly ISearchService _searchService;

        public VectorSearch(ISearchService searchService)
        {
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        }

        /// <summary>
        /// Name of the search index
        /// </summary>
        public string IndexName { get; set; }

        /// <summary>
        /// Text query (will be embedded automatically)
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Pre-computed embedding vector (optional, if provided Query is ignored)
        /// </summary>
        public float[] Embedding { get; set; }

        /// <summary>
        /// Number of results to return
        /// </summary>
        public int TopK { get; set; } = 5;

        /// <summary>
        /// OData filter expression
        /// </summary>
        public string Filter { get; set; }

        // Outputs

        /// <summary>
        /// Search results
        /// </summary>
        public IList<SearchResult> Results { get; set; }

        /// <summary>
        /// Total count of matching documents
        /// </summary>
        public long? TotalCount { get; set; }

        public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
        {
            if (string.IsNullOrEmpty(IndexName))
            {
                throw new InvalidOperationException("IndexName is required for vector search");
            }

            SearchResults searchResults;

            if (Embedding != null && Embedding.Length > 0)
            {
                searchResults = await _searchService.SearchByVectorAsync(
                    IndexName,
                    Embedding,
                    TopK,
                    Filter,
                    context.CancellationToken);
            }
            else if (!string.IsNullOrEmpty(Query))
            {
                searchResults = await _searchService.SearchAsync(
                    IndexName,
                    Query,
                    TopK,
                    Filter,
                    context.CancellationToken);
            }
            else
            {
                throw new InvalidOperationException("Either Query or Embedding is required for vector search");
            }

            Results = searchResults.Results;
            TotalCount = searchResults.TotalCount;

            return ExecutionResult.Next();
        }
    }
}
