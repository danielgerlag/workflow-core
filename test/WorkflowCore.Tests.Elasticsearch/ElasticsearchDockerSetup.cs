using System;
using System.Threading.Tasks;
using Squadron;
using Xunit;

namespace WorkflowCore.Tests.Elasticsearch
{
    public class ElasticsearchDockerSetup : IAsyncLifetime
    {
        private readonly ElasticsearchResource _elasticsearchResource;
        public static string ConnectionString { get; set; }

        public ElasticsearchDockerSetup()
        {
            _elasticsearchResource = new ElasticsearchResource();
        }

        public async Task InitializeAsync()
        {
            await _elasticsearchResource.InitializeAsync();
            ConnectionString = $"http://localhost:{_elasticsearchResource.Instance.HostPort}";
        }

        public Task DisposeAsync()
        {
            return _elasticsearchResource.DisposeAsync();
        }
    }

    [CollectionDefinition("Elasticsearch collection")]
    public class ElasticsearchCollection : ICollectionFixture<ElasticsearchDockerSetup>
    {
    }
}