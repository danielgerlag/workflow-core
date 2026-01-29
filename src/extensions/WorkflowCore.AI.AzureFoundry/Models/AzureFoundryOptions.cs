using System;
using Azure.Core;

namespace WorkflowCore.AI.AzureFoundry.Models
{
    public class AzureFoundryOptions
    {
        /// <summary>
        /// Azure AI Foundry endpoint URL (e.g., "https://myresource.services.ai.azure.com")
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Azure AI Foundry project name
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// API key for authentication (if not using Azure credentials)
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Default model to use for chat completions (e.g., "gpt-4o")
        /// </summary>
        public string DefaultModel { get; set; } = "gpt-4o";

        /// <summary>
        /// Default model to use for embeddings (e.g., "text-embedding-3-small")
        /// </summary>
        public string DefaultEmbeddingModel { get; set; } = "text-embedding-3-small";

        /// <summary>
        /// Azure credential for authentication. If null and ApiKey is null, DefaultAzureCredential will be used.
        /// </summary>
        public TokenCredential Credential { get; set; }

        /// <summary>
        /// Default temperature for LLM calls (0.0 - 2.0)
        /// </summary>
        public float DefaultTemperature { get; set; } = 0.7f;

        /// <summary>
        /// Default maximum tokens for LLM responses
        /// </summary>
        public int DefaultMaxTokens { get; set; } = 4096;

        /// <summary>
        /// Azure AI Search endpoint for vector search operations
        /// </summary>
        public string SearchEndpoint { get; set; }

        /// <summary>
        /// Azure AI Search API key (optional, uses DefaultAzureCredential if not provided)
        /// </summary>
        public string SearchApiKey { get; set; }
    }
}
