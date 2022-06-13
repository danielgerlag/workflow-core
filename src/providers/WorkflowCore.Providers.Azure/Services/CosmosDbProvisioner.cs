using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using WorkflowCore.Providers.Azure.Interface;

namespace WorkflowCore.Providers.Azure.Services
{
    public class CosmosDbProvisioner : ICosmosDbProvisioner
    {
        private readonly ICosmosClientFactory _clientFactory;
        private readonly CosmosDbStorageOptions _cosmosDbStorageOptions;

        public CosmosDbProvisioner(
            ICosmosClientFactory clientFactory,
            CosmosDbStorageOptions cosmosDbStorageOptions)
        {
            _clientFactory = clientFactory;
            _cosmosDbStorageOptions = cosmosDbStorageOptions;
        }

        public async Task Provision(string dbId, CancellationToken cancellationToken = default)
        {
            var dbResp = await _clientFactory.GetCosmosClient().CreateDatabaseIfNotExistsAsync(dbId, cancellationToken: cancellationToken);
            var wfIndexPolicy = new IndexingPolicy();
            wfIndexPolicy.IncludedPaths.Add(new IncludedPath { Path = @"/*" });
            wfIndexPolicy.ExcludedPaths.Add(new ExcludedPath { Path = @"/ExecutionPointers/?" });

            Task.WaitAll(
                dbResp.Database.CreateContainerIfNotExistsAsync(new ContainerProperties(_cosmosDbStorageOptions.WorkflowContainerName, @"/id")
                {
                    IndexingPolicy = wfIndexPolicy
                }),
                dbResp.Database.CreateContainerIfNotExistsAsync(new ContainerProperties(_cosmosDbStorageOptions.EventContainerName, @"/id")),
                dbResp.Database.CreateContainerIfNotExistsAsync(new ContainerProperties(_cosmosDbStorageOptions.SubscriptionContainerName, @"/id"))
            );
        }
    }
}
