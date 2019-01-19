using System;
using Microsoft.Extensions.Logging;
using Nest;
using WorkflowCore.Models;
using WorkflowCore.Providers.Elasticsearch.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseElasticsearch(this WorkflowOptions options, ConnectionSettings settings, string indexName)
        {
            options.UseSearchIndex(sp => new ElasticsearchIndexer(settings, indexName, sp.GetService<ILoggerFactory>()));
            return options;
        }
    }
}
