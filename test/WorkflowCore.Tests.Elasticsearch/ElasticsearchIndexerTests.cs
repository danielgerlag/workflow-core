using System;
using Microsoft.Extensions.Logging;
using Nest;
using WorkflowCore.IntegrationTests;
using WorkflowCore.Interface;
using WorkflowCore.Providers.Elasticsearch.Services;
using Xunit;

namespace WorkflowCore.Tests.Elasticsearch
{
    [Collection("Elasticsearch collection")]
    public class ElasticsearchIndexerTests : SearchIndexTests
    {
        private readonly string _indexName = $"workflowcore-tests-{Guid.NewGuid():N}";

        public ElasticsearchIndexerTests(ElasticsearchDockerSetup dockerSetup)
        {
            _ = dockerSetup;
        }

        protected override ISearchIndex CreateService()
        {
            var settings = new ConnectionSettings(new Uri(ElasticsearchDockerSetup.ConnectionString));
            return new ElasticsearchIndexer(settings, _indexName, new LoggerFactory());
        }

        protected override void WaitUntilSearchable()
        {
            var client = new ElasticClient(new ConnectionSettings(new Uri(ElasticsearchDockerSetup.ConnectionString)));
            var refresh = client.Indices.Refresh(_indexName);
            if (!refresh.IsValid)
            {
                throw new InvalidOperationException(
                    $"Elasticsearch refresh failed for '{_indexName}': {refresh.DebugInformation}");
            }
        }
    }
}
