using System;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Kinesis;
using Amazon.Runtime;
using Amazon.SQS;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Providers.AWS.Interface;
using WorkflowCore.Providers.AWS.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseAwsSimpleQueueService(this WorkflowOptions options, AWSCredentials credentials, AmazonSQSConfig config, string queuesPrefix = "workflowcore")
        {
            var sqsClient = new AmazonSQSClient(credentials, config);
            return options.UseAwsSimpleQueueServiceWithProvisionedClient(sqsClient, queuesPrefix);
        }

        public static WorkflowOptions UseAwsSimpleQueueServiceWithProvisionedClient(this WorkflowOptions options, AmazonSQSClient sqsClient, string queuesPrefix = "workflowcore")
        {
            options.UseQueueProvider(sp => new SQSQueueProvider(sqsClient, sp.GetService<ILoggerFactory>(), queuesPrefix));
            return options;
        }

        public static WorkflowOptions UseAwsDynamoLocking(this WorkflowOptions options, AWSCredentials credentials, AmazonDynamoDBConfig config, string tableName)
        {
            var dbClient = new AmazonDynamoDBClient(credentials, config);
            return options.UseAwsDynamoLockingWithProvisionedClient(dbClient, tableName);
        }

        public static WorkflowOptions UseAwsDynamoLockingWithProvisionedClient (this WorkflowOptions options, AmazonDynamoDBClient dynamoClient, string tableName)
        {
            options.UseDistributedLockManager(sp => new DynamoLockProvider(dynamoClient, tableName, sp.GetService<ILoggerFactory>(), sp.GetService<IDateTimeProvider>()));
            return options;
        }

        public static WorkflowOptions UseAwsDynamoPersistence(this WorkflowOptions options, AWSCredentials credentials, AmazonDynamoDBConfig config, string tablePrefix)
        {
            var dbClient = new AmazonDynamoDBClient(credentials, config);
            return options.UseAwsDynamoPersistenceWithProvisionedClient(dbClient, tablePrefix);
        }

        public static WorkflowOptions UseAwsDynamoPersistenceWithProvisionedClient(this WorkflowOptions options, AmazonDynamoDBClient dynamoClient, string tablePrefix)
        {
            options.Services.AddTransient<IDynamoDbProvisioner>(sp => new DynamoDbProvisioner(dynamoClient, tablePrefix, sp.GetService<ILoggerFactory>()));
            options.UsePersistence(sp => new DynamoPersistenceProvider(dynamoClient, sp.GetService<IDynamoDbProvisioner>(), tablePrefix, sp.GetService<ILoggerFactory>()));
            return options;
        }

        public static WorkflowOptions UseAwsKinesis(this WorkflowOptions options, AWSCredentials credentials, RegionEndpoint region, string appName, string streamName)
        {
            var kinesisClient = new AmazonKinesisClient(credentials, region);
            var dynamoClient = new AmazonDynamoDBClient(credentials, region);
            
            return options.UseAwsKinesisWithProvisionedClients(kinesisClient, dynamoClient,appName, streamName);

        }

        public static WorkflowOptions UseAwsKinesisWithProvisionedClients(this WorkflowOptions options, AmazonKinesisClient kinesisClient, AmazonDynamoDBClient dynamoDbClient, string appName, string streamName)
        {
            options.Services.AddTransient<IKinesisTracker>(sp => new KinesisTracker(dynamoDbClient, "workflowcore_kinesis", sp.GetService<ILoggerFactory>()));
            options.Services.AddTransient<IKinesisStreamConsumer>(sp => new KinesisStreamConsumer(kinesisClient, sp.GetService<IKinesisTracker>(), sp.GetService<IDistributedLockProvider>(), sp.GetService<ILoggerFactory>(), sp.GetService<IDateTimeProvider>()));
            options.UseEventHub(sp => new KinesisProvider(kinesisClient, appName, streamName, sp.GetService<IKinesisStreamConsumer>(), sp.GetService<ILoggerFactory>()));
            return options;
        }
    }
}
