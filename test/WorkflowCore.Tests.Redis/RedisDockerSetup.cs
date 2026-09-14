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
            // Optional: point at an already-running Redis (e.g. CI without Docker).
            var fromEnv = Environment.GetEnvironmentVariable("WORKFLOWCORE_REDIS");
            if (!string.IsNullOrEmpty(fromEnv))
            {
                ConnectionString = fromEnv;
                return;
            }

            await _redisResource.InitializeAsync();
            ConnectionString = _redisResource.ConnectionString;
        }

        public Task DisposeAsync()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WORKFLOWCORE_REDIS")))
                return Task.CompletedTask;

            return _redisResource.DisposeAsync();
        }
    }

    [CollectionDefinition("Redis collection")]
    public class RedisCollection : ICollectionFixture<RedisDockerSetup>
    {
    }
}