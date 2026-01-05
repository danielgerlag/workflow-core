using System;
using Azure.Core;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Providers.Azure.Interface;
using WorkflowCore.Providers.Azure.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseAzureSynchronization(this WorkflowOptions options, string connectionString)
        {
            options.UseQueueProvider(sp => new AzureStorageQueueProvider(connectionString, sp.GetService<ILoggerFactory>()));
            options.UseDistributedLockManager(sp => new AzureLockManager(connectionString, sp.GetService<ILoggerFactory>()));
            return options;
        }

        public static WorkflowOptions UseAzureSynchronization(this WorkflowOptions options, Uri blobEndpoint, Uri queueEndpoint, TokenCredential tokenCredential)
        {
            options.UseQueueProvider(sp => new AzureStorageQueueProvider(queueEndpoint, tokenCredential, sp.GetService<ILoggerFactory>()));
            options.UseDistributedLockManager(sp => new AzureLockManager(blobEndpoint, tokenCredential, sp.GetService<ILoggerFactory>()));
            return options;
        }

        public static WorkflowOptions UseAzureServiceBusEventHub(
            this WorkflowOptions options,
            string connectionString,
            string topicName,
            string subscriptionName)
        {
            options.UseEventHub(sp => new ServiceBusLifeCycleEventHub(
                connectionString, topicName, subscriptionName, sp.GetService<ILoggerFactory>()));

            return options;
        }

        public static WorkflowOptions UseAzureServiceBusEventHub(
            this WorkflowOptions options,
            string fullyQualifiedNamespace,
            TokenCredential tokenCredential,
            string topicName,
            string subscriptionName)
        {
            options.UseEventHub(sp => new ServiceBusLifeCycleEventHub(
                fullyQualifiedNamespace, tokenCredential, topicName, subscriptionName, sp.GetService<ILoggerFactory>()));

            return options;
        }

        public static WorkflowOptions UseCosmosDbPersistence(
            this WorkflowOptions options,
            string connectionString,
            string databaseId,
            CosmosDbStorageOptions cosmosDbStorageOptions = null,
            CosmosClientOptions clientOptions = null)
        {
            if (cosmosDbStorageOptions == null)
            {
                cosmosDbStorageOptions = new CosmosDbStorageOptions();
            }

            options.Services.AddSingleton<ICosmosClientFactory>(sp => new CosmosClientFactory(connectionString, clientOptions));
            options.Services.AddTransient<ICosmosDbProvisioner>(sp => new CosmosDbProvisioner(sp.GetService<ICosmosClientFactory>(), cosmosDbStorageOptions));
            options.Services.AddSingleton<IWorkflowPurger>(sp => new WorkflowPurger(sp.GetService<ICosmosClientFactory>(), databaseId, cosmosDbStorageOptions));
            options.UsePersistence(sp => new CosmosDbPersistenceProvider(sp.GetService<ICosmosClientFactory>(), databaseId, sp.GetService<ICosmosDbProvisioner>(), cosmosDbStorageOptions));
            return options;
        }

        public static WorkflowOptions UseCosmosDbPersistence(
            this WorkflowOptions options,
            CosmosClient client,
            string databaseId,
            CosmosDbStorageOptions cosmosDbStorageOptions = null,
            CosmosClientOptions clientOptions = null)
        {
            if (cosmosDbStorageOptions == null)
            {
                cosmosDbStorageOptions = new CosmosDbStorageOptions();
            }

            options.Services.AddSingleton<ICosmosClientFactory>(sp => new CosmosClientFactory(client));
            options.Services.AddTransient<ICosmosDbProvisioner>(sp => new CosmosDbProvisioner(sp.GetService<ICosmosClientFactory>(), cosmosDbStorageOptions));
            options.Services.AddSingleton<IWorkflowPurger>(sp => new WorkflowPurger(sp.GetService<ICosmosClientFactory>(), databaseId, cosmosDbStorageOptions));
            options.UsePersistence(sp => new CosmosDbPersistenceProvider(sp.GetService<ICosmosClientFactory>(), databaseId, sp.GetService<ICosmosDbProvisioner>(), cosmosDbStorageOptions));
            return options;
        }

        public static WorkflowOptions UseCosmosDbPersistence(
            this WorkflowOptions options,
            string accountEndpoint,
            TokenCredential tokenCredential,
            string databaseId,
            CosmosDbStorageOptions cosmosDbStorageOptions = null)
        {
            if (cosmosDbStorageOptions == null)
            {
                cosmosDbStorageOptions = new CosmosDbStorageOptions();
            }

            options.Services.AddSingleton<ICosmosClientFactory>(sp => new CosmosClientFactory(accountEndpoint, tokenCredential));
            options.Services.AddTransient<ICosmosDbProvisioner>(sp => new CosmosDbProvisioner(sp.GetService<ICosmosClientFactory>(), cosmosDbStorageOptions));
            options.Services.AddSingleton<IWorkflowPurger>(sp => new WorkflowPurger(sp.GetService<ICosmosClientFactory>(), databaseId, cosmosDbStorageOptions));
            options.UsePersistence(sp => new CosmosDbPersistenceProvider(sp.GetService<ICosmosClientFactory>(), databaseId, sp.GetService<ICosmosDbProvisioner>(), cosmosDbStorageOptions));
            return options;
        }
    }
}
