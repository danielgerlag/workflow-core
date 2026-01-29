using System;
using Azure;
using Azure.AI.Inference;
using Azure.Identity;
using Microsoft.Extensions.Options;
using WorkflowCore.AI.AzureFoundry.Models;

namespace WorkflowCore.AI.AzureFoundry.Services
{
    /// <summary>
    /// Factory for creating Azure AI Foundry SDK clients.
    /// Supports Azure AI Foundry (services.ai.azure.com) endpoints.
    /// </summary>
    public class AzureFoundryClientFactory
    {
        private readonly AzureFoundryOptions _options;

        public AzureFoundryClientFactory(IOptions<AzureFoundryOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Create a ChatCompletionsClient
        /// </summary>
        public ChatCompletionsClient CreateChatClient()
        {
            var endpoint = BuildEndpoint();
            
            if (!string.IsNullOrEmpty(_options.ApiKey))
            {
                return new ChatCompletionsClient(endpoint, new AzureKeyCredential(_options.ApiKey));
            }
            
            var credential = _options.Credential ?? new DefaultAzureCredential();
            return new ChatCompletionsClient(endpoint, credential);
        }

        /// <summary>
        /// Create an EmbeddingsClient
        /// </summary>
        public EmbeddingsClient CreateEmbeddingsClient()
        {
            var endpoint = BuildEndpoint();
            
            if (!string.IsNullOrEmpty(_options.ApiKey))
            {
                return new EmbeddingsClient(endpoint, new AzureKeyCredential(_options.ApiKey));
            }
            
            var credential = _options.Credential ?? new DefaultAzureCredential();
            return new EmbeddingsClient(endpoint, credential);
        }

        private Uri BuildEndpoint()
        {
            var baseEndpoint = _options.Endpoint.TrimEnd('/');
            
            // For Azure AI Foundry (services.ai.azure.com), append /models
            // The SDK will then call /models/chat/completions or /models/embeddings
            if (baseEndpoint.Contains("services.ai.azure.com") && !baseEndpoint.EndsWith("/models"))
            {
                return new Uri($"{baseEndpoint}/models");
            }
            
            return new Uri(baseEndpoint);
        }
    }
}
