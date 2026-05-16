using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Models;
using WorkflowCore.AI.AzureFoundry.Primitives;
using WorkflowCore.AI.AzureFoundry.Services;

namespace WorkflowCore.AI.AzureFoundry.ServiceExtensions
{
    /// <summary>
    /// Extension methods for adding Azure AI Foundry services to the DI container
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add Azure AI Foundry services to WorkflowCore
        /// </summary>
        public static IServiceCollection AddAzureFoundry(
            this IServiceCollection services,
            Action<AzureFoundryOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            services.Configure(configure);

            // Core services
            services.AddSingleton<AzureFoundryClientFactory>();
            services.AddSingleton<IToolRegistry, ToolRegistry>();
            services.AddSingleton<IConversationStore, InMemoryConversationStore>();

            // AI services
            services.AddTransient<IChatCompletionService, ChatCompletionService>();
            services.AddTransient<IEmbeddingService, EmbeddingService>();
            services.AddTransient<ISearchService, SearchService>();

            // Step bodies
            services.AddTransient<ChatCompletion>();
            services.AddTransient<GenerateEmbedding>();
            services.AddTransient<VectorSearch>();
            services.AddTransient<ExecuteTool>();
            services.AddTransient<AgentLoop>();
            services.AddTransient<HumanReview>();

            return services;
        }

        /// <summary>
        /// Register a tool with the tool registry
        /// </summary>
        public static IServiceCollection AddAgentTool<TTool>(this IServiceCollection services)
            where TTool : class, IAgentTool
        {
            services.AddTransient<TTool>();
            services.AddTransient<IAgentTool, TTool>();
            return services;
        }

        /// <summary>
        /// Use a custom conversation store implementation
        /// </summary>
        public static IServiceCollection UseConversationStore<TStore>(this IServiceCollection services)
            where TStore : class, IConversationStore
        {
            services.AddSingleton<IConversationStore, TStore>();
            return services;
        }
    }
}
