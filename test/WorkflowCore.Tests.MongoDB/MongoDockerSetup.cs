using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Squadron;
using WorkflowCore.UnitTests;
using Xunit;

namespace WorkflowCore.Tests.MongoDB
{
    public class MongoDockerSetup : IAsyncLifetime
    {
        private readonly MongoReplicaSetResource _mongoResource;
        public static string ConnectionString { get; set; }

        public MongoDockerSetup()
        {
            _mongoResource = new MongoReplicaSetResource();
        }

        public async Task InitializeAsync()
        {
            await _mongoResource.InitializeAsync();
            ConnectionString = _mongoResource.ConnectionString;
            BsonSerializer.TryRegisterSerializer(new ObjectSerializer(type =>
                ObjectSerializer.DefaultAllowedTypes(type) || type.FullName.StartsWith("WorkflowCore.")));
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