using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Net;
using Docker.Testify;
using StackExchange.Redis;
using Xunit;

namespace WorkflowCore.Tests.Redis
{    
    public class RedisDockerSetup : DockerSetup
    {
        public static string ConnectionString { get; set; }

        public override string ImageName => @"redis";
        public override int InternalPort => 6379;

        public override void PublishConnectionInfo()
        {
            ConnectionString = $"localhost:{ExternalPort}";
        }

        public override bool TestReady()
        {
            try
            {
                var multiplexer = ConnectionMultiplexer.Connect($"localhost:{ExternalPort}");
                return multiplexer.IsConnected;
            }
            catch
            {
                return false;
            }

        }
    }

    [CollectionDefinition("Redis collection")]
    public class RedisCollection : ICollectionFixture<RedisDockerSetup>
    {        
    }

}
