using System;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Providers.AWS.Services;
using WorkflowCore.UnitTests;
using Xunit;

namespace WorkflowCore.Tests.DynamoDB
{
    [Collection("DynamoDb collection")]
    public class DynamoPersistenceProviderFixture : BasePersistenceFixture
    {
        DynamoDbDockerSetup _dockerSetup;
        private IPersistenceProvider _subject;

        public DynamoPersistenceProviderFixture(DynamoDbDockerSetup dockerSetup)
        {
            _dockerSetup = dockerSetup;
        }

        protected override IPersistenceProvider Subject
        {
            get
            {
                if (_subject == null)
                {
                    var cfg = new AmazonDynamoDBConfig { ServiceURL = DynamoDbDockerSetup.ConnectionString };
                    var provisioner = new DynamoDbProvisioner(DynamoDbDockerSetup.Credentials, cfg, "unittests", new LoggerFactory());
                    var client = new DynamoPersistenceProvider(DynamoDbDockerSetup.Credentials, cfg, provisioner, "unittests", new LoggerFactory());
                    client.EnsureStoreExists();
                    _subject = client;
                }
                return _subject;
            }
        }
    }
}
