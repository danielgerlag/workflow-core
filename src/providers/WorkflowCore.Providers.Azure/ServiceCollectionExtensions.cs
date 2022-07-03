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
    }
}
