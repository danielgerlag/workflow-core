using System;
using System.Threading.Tasks;
using Squadron;
using Xunit;

namespace WorkflowCore.Tests.Redis
{
    public class RedisDockerSetup : IAsyncLifetime
    {
        private readonly RedisResource _redisResource;
        public static string ConnectionString { get; set; }

        public RedisDockerSetup()
        {
            _redisResource = new RedisResource();
        }

        public async Task InitializeAsync()
        {
            await _redisResource.InitializeAsync();
            ConnectionString = _redisResource.ConnectionString;
        }

        public Task DisposeAsync()
        {
            return _redisResource.DisposeAsync();
        }
    }

    [CollectionDefinition("Redis collection")]
    public class RedisCollection : ICollectionFixture<RedisDockerSetup>
    {
    }
}