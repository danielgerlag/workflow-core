using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Docker.Testify;
using Xunit;
using Amazon.DynamoDBv2;
using Amazon.Runtime;

namespace WorkflowCore.Tests.DynamoDB
{    
    public class DynamoDbDockerSetup : DockerSetup
    {
        public static string ConnectionString { get; set; }

        public static AWSCredentials Credentials => new EnvironmentVariablesAWSCredentials();

        public override string ImageName => @"amazon/dynamodb-local";
        public override int InternalPort => 8000;
        public override TimeSpan TimeOut => TimeSpan.FromSeconds(120);

        public override void PublishConnectionInfo()
        {
            ConnectionString = $"http://localhost:{ExternalPort}";
        }

        public override bool TestReady()
        {
            try
            {
                AmazonDynamoDBConfig clientConfig = new AmazonDynamoDBConfig
                {
                    ServiceURL = $"http://localhost:{ExternalPort}"
                };
                AmazonDynamoDBClient client = new AmazonDynamoDBClient(clientConfig);
                var resp = client.ListTablesAsync().Result;

                return resp.HttpStatusCode == HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }

        }
    }

    [CollectionDefinition("DynamoDb collection")]
    public class DynamoDbCollection : ICollectionFixture<DynamoDbDockerSetup>
    {        
    }

}
