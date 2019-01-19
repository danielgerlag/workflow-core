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
        ElasticsearchDockerSetup _dockerSetup;

        public ElasticsearchIndexerTests(ElasticsearchDockerSetup dockerSetup)
        {
            _dockerSetup = dockerSetup;
        }

        protected override ISearchIndex CreateService()
        {
            var settings = new ConnectionSettings(new Uri(ElasticsearchDockerSetup.ConnectionString));
            return new ElasticsearchIndexer(settings, "workflowcore.tests", new LoggerFactory());
        }
    }
}
