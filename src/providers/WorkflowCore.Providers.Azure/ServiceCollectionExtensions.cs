using Microsoft.Extensions.Logging;
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

        public static WorkflowOptions UseCosmosDbPersistence(this WorkflowOptions options, string connectionString, string databaseId)
        {
            options.Services.AddTransient<ICosmosDbProvisioner>(sp => new CosmosDbProvisioner(connectionString, sp.GetService<ILoggerFactory>()));
            options.UsePersistence(sp => new CosmosDbPersistenceProvider(connectionString, databaseId, sp.GetService<ICosmosDbProvisioner>()));
            return options;
        }
    }
}
