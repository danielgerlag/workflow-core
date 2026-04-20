using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Models;

namespace WorkflowCore.AI.AzureFoundry.Services
{
    /// <summary>
    /// Service for vector search operations using Azure AI Search
    /// </summary>
    public class SearchService : ISearchService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly AzureFoundryOptions _options;
        private readonly ILogger<SearchService> _logger;

        public SearchService(
            IEmbeddingService embeddingService,
            IOptions<AzureFoundryOptions> options,
            ILogger<SearchService> logger)
        {
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SearchResults> SearchAsync(
            string indexName,
            string query,
            int topK = 5,
            string filter = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(query))
                throw new ArgumentException("Query cannot be null or empty", nameof(query));

            _logger.LogDebug("Generating embedding for search query");
            var embeddingResponse = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken: cancellationToken);

            return await SearchByVectorAsync(indexName, embeddingResponse.Embedding, topK, filter, cancellationToken);
        }

        public async Task<SearchResults> SearchByVectorAsync(
            string indexName,
            float[] embedding,
            int topK = 5,
            string filter = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(indexName))
                throw new ArgumentException("Index name cannot be null or empty", nameof(indexName));

            if (embedding == null || embedding.Length == 0)
                throw new ArgumentException("Embedding cannot be null or empty", nameof(embedding));

            if (string.IsNullOrEmpty(_options.SearchEndpoint))
                throw new InvalidOperationException("Search endpoint is not configured");

            var searchClient = CreateSearchClient(indexName);

            var vectorQuery = new VectorizedQuery(embedding.Select(f => f).ToArray())
            {
                KNearestNeighborsCount = topK,
                Fields = { "contentVector" }
            };

            var searchOptions = new SearchOptions
            {
                VectorSearch = new VectorSearchOptions
                {
                    Queries = { vectorQuery }
                },
                Size = topK,
                Select = { "id", "content", "title" }
            };

            if (!string.IsNullOrEmpty(filter))
            {
                searchOptions.Filter = filter;
            }

            _logger.LogDebug("Executing vector search on index {IndexName}", indexName);

            var response = await searchClient.SearchAsync<SearchDocument>(null, searchOptions, cancellationToken);
            var results = new SearchResults { Query = "vector search" };

            await foreach (var result in response.Value.GetResultsAsync())
            {
                var searchResult = new SearchResult
                {
                    DocumentId = result.Document.GetString("id"),
                    Score = result.Score ?? 0,
                    Content = result.Document.GetString("content"),
                    Title = result.Document.GetString("title")
                };

                foreach (var field in result.Document)
                {
                    if (field.Key != "id" && field.Key != "content" && field.Key != "title" && field.Key != "contentVector")
                    {
                        searchResult.Fields[field.Key] = field.Value;
                    }
                }

                results.Results.Add(searchResult);
            }

            results.TotalCount = response.Value.TotalCount;
            return results;
        }

        private SearchClient CreateSearchClient(string indexName)
        {
            var endpoint = new Uri(_options.SearchEndpoint);

            if (!string.IsNullOrEmpty(_options.SearchApiKey))
            {
                return new SearchClient(endpoint, indexName, new AzureKeyCredential(_options.SearchApiKey));
            }

            return new SearchClient(endpoint, indexName, new DefaultAzureCredential());
        }
    }
}
