using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using Squadron;
using Xunit;

namespace WorkflowCore.Tests.MongoDB
{
    public class MongoDockerSetup : IAsyncLifetime
    {
        private readonly MongoResource _mongoResource;
        public static string ConnectionString { get; set; }

        public MongoDockerSetup()
        {
            _mongoResource = new MongoResource();
        }

        public async Task InitializeAsync()
        {
            await _mongoResource.InitializeAsync();
            ConnectionString = _mongoResource.ConnectionString;
        }

        public Task DisposeAsync()
        {
            return _mongoResource.DisposeAsync();
        }
    }

    [CollectionDefinition("Mongo collection")]
    public class MongoCollection : ICollectionFixture<MongoDockerSetup>
    {
    }
}