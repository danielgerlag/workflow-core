using System.Collections.Generic;

namespace WorkflowCore.AI.AzureFoundry.Models
{
    /// <summary>
    /// Result from a vector search operation
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// Unique identifier of the document
        /// </summary>
        public string DocumentId { get; set; }

        /// <summary>
        /// Relevance score (higher is more relevant)
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Document content
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Document title or name
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Additional fields from the document
        /// </summary>
        public IDictionary<string, object> Fields { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Collection of search results
    /// </summary>
    public class SearchResults
    {
        /// <summary>
        /// Individual search results
        /// </summary>
        public IList<SearchResult> Results { get; set; } = new List<SearchResult>();

        /// <summary>
        /// Total number of matching documents
        /// </summary>
        public long? TotalCount { get; set; }

        /// <summary>
        /// The query that produced these results
        /// </summary>
        public string Query { get; set; }
    }
}
