using System;
using System.Net;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace WorkflowCore.Tests.Elasticsearch
{
    public class ElasticsearchDockerSetup : IAsyncLifetime
    {
        // ES 7.17 matches NEST 7.x used by the production indexer. Pin the tag so
        // `latest` cannot drift onto ES 8/9 (security/TLS on by default).
        // docker.elastic.co is the official 7.17 distribution; Docker Hub's
        // elasticsearch official image only publishes 8.x/9.x now.
        private const string Image = "docker.elastic.co/elasticsearch/elasticsearch:7.17.29";
        private const ushort HttpPort = 9200;

        private readonly IContainer _container;

        public static string ConnectionString { get; private set; }

        public ElasticsearchDockerSetup()
        {
            _container = new ContainerBuilder()
                .WithImage(Image)
                .WithPortBinding(HttpPort, true)
                .WithEnvironment("discovery.type", "single-node")
                .WithEnvironment("cluster.name", "workflowcore-tests")
                .WithEnvironment("xpack.security.enabled", "false")
                .WithEnvironment("xpack.ml.enabled", "false")
                .WithEnvironment("ingest.geoip.downloader.enabled", "false")
                .WithEnvironment("node.store.allow_mmap", "false")
                .WithEnvironment("cluster.routing.allocation.disk.threshold_enabled", "false")
                .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(
                        request => request
                            .ForPort(HttpPort)
                            .ForPath("/_cluster/health")
                            .ForStatusCode(HttpStatusCode.OK),
                        strategy => strategy.WithTimeout(TimeSpan.FromMinutes(3))))
                .Build();
        }

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
            ConnectionString = $"http://localhost:{_container.GetMappedPublicPort(HttpPort)}";
        }

        public async Task DisposeAsync()
        {
            await _container.DisposeAsync();
        }
    }

    [CollectionDefinition("Elasticsearch collection")]
    public class ElasticsearchCollection : ICollectionFixture<ElasticsearchDockerSetup>
    {
    }
}
