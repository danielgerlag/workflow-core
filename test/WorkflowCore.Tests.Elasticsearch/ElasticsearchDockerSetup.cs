using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Docker.Testify;
using Nest;
using Xunit;

namespace WorkflowCore.Tests.Elasticsearch
{
    public class ElasticsearchDockerSetup : DockerSetup
    {
        public static string ConnectionString { get; set; }
        
        public override string ImageName => @"elasticsearch";
        public override string ImageTag => "6.5.4";
        public override int InternalPort => 9200;

        public override void PublishConnectionInfo()
        {
            ConnectionString = $"http://localhost:{ExternalPort}";
        }

        public override bool TestReady()
        {
            try
            {
                var client = new ElasticClient(new ConnectionSettings(new Uri($"http://localhost:{ExternalPort}")));
                var ping = client.Ping();
                return ping.IsValid;
            }
            catch
            {
                return false;
            }

        }
    }

    [CollectionDefinition("Elasticsearch collection")]
    public class DynamoDbCollection : ICollectionFixture<ElasticsearchDockerSetup>
    {
    }
}
