using System;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.SQS;
using Microsoft.Extensions.Logging;
using WorkflowCore.Models;
using WorkflowCore.Providers.AWS.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseAwsSimpleQueueService(this WorkflowOptions options, AWSCredentials credentials, AmazonSQSConfig config)
        {
            options.UseQueueProvider(sp => new SQSQueueProvider(credentials, config, sp.GetService<ILoggerFactory>()));
            return options;
        }

        public static WorkflowOptions UseAwsDynamoLocking(this WorkflowOptions options, AWSCredentials credentials, AmazonDynamoDBConfig config, string tableName)
        {
            options.UseDistributedLockManager(sp => new DynamoLockProvider(credentials, config, tableName, sp.GetService<ILoggerFactory>()));
            return options;
        }

        public static WorkflowOptions UseAwsDynamoPersistence(this WorkflowOptions options, AWSCredentials credentials, AmazonDynamoDBConfig config, string tablePrefix)
        {
            options.Services.AddTransient<IDynamoDbProvisioner>(sp => new DynamoDbProvisioner(credentials, config, tablePrefix, sp.GetService<ILoggerFactory>()));
            options.UsePersistence(sp => new DynamoPersistenceProvider(credentials, config, sp.GetService<IDynamoDbProvisioner>(), tablePrefix, sp.GetService<ILoggerFactory>()));
            return options;
        }
    }
}
