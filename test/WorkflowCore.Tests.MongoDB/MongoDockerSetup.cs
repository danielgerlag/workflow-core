using Docker.Testify;
using MongoDB.Driver;
using Squadron;
using Xunit;

namespace WorkflowCore.Tests.MongoDB
{
    public class MongoDockerSetup : DockerSetup
    {
        public static string ConnectionString { get; set; }

        public override string ImageName => "mongo";
        public override int InternalPort => 27017;

        public override void PublishConnectionInfo()
        {
            ConnectionString = $"mongodb://localhost:{ExternalPort}";
        }

        public override bool TestReady()
        {
            try
            {
                var client = new MongoClient($"mongodb://localhost:{ExternalPort}");
                client.ListDatabases();
                return true;
            }
            catch
            {
                return false;
            }

        }
    }

    [CollectionDefinition("Mongo collection")]
    public class MongoCollection : ICollectionFixture<MongoDockerSetup>
    {        
    }

    internal class CollectionDefinitionNames
    {
        public const string Squadron = "Squadron.MongoDB";
    }

    [CollectionDefinition(CollectionDefinitionNames.Squadron)]
    public class MongoResourceFixture : ICollectionFixture<MongoResource>
    {
    }
}
