using System;
using Amazon;
using Amazon.DynamoDBv2;
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
            options.UseQueueProvider(sp => new SQSQueueProvider(credentials, config, sp.GetService<ILoggerFactory>(), queuesPrefix));
            return options;
        }

        public static WorkflowOptions UseAwsDynamoLocking(this WorkflowOptions options, AWSCredentials credentials, AmazonDynamoDBConfig config, string tableName)
        {
            options.UseDistributedLockManager(sp => new DynamoLockProvider(credentials, config, tableName, sp.GetService<ILoggerFactory>(), sp.GetService<IDateTimeProvider>()));
            return options;
        }

        public static WorkflowOptions UseAwsDynamoPersistence(this WorkflowOptions options, AWSCredentials credentials, AmazonDynamoDBConfig config, string tablePrefix)
        {
            options.Services.AddTransient<IDynamoDbProvisioner>(sp => new DynamoDbProvisioner(credentials, config, tablePrefix, sp.GetService<ILoggerFactory>()));
            options.UsePersistence(sp => new DynamoPersistenceProvider(credentials, config, sp.GetService<IDynamoDbProvisioner>(), tablePrefix, sp.GetService<ILoggerFactory>()));
            return options;
        }

        public static WorkflowOptions UseAwsKinesis(this WorkflowOptions options, AWSCredentials credentials, RegionEndpoint region, string appName, string streamName)
        {
            options.Services.AddTransient<IKinesisTracker>(sp => new KinesisTracker(credentials, region, "workflowcore_kinesis", sp.GetService<ILoggerFactory>()));
            options.Services.AddTransient<IKinesisStreamConsumer>(sp => new KinesisStreamConsumer(credentials, region, sp.GetService<IKinesisTracker>(), sp.GetService<IDistributedLockProvider>(), sp.GetService<ILoggerFactory>(), sp.GetService<IDateTimeProvider>()));
            options.UseEventHub(sp => new KinesisProvider(credentials, region, appName, streamName, sp.GetService<IKinesisStreamConsumer>(), sp.GetService<ILoggerFactory>()));
            return options;
        }
    }
}
