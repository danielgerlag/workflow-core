using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using WorkflowCore.Providers.Azure.Interface;

namespace WorkflowCore.Providers.Azure.Services
{
    public class CosmosDbProvisioner : ICosmosDbProvisioner
    {

        private ICosmosDbClient _client;

        public CosmosDbProvisioner(ICosmosDbClient client, ILoggerFactory loggerFactory)
        {
            _client = client;
        }

        public async Task Provision(string dbId)
        {
            var dbResp = await _client.GetCosmosClient().CreateDatabaseIfNotExistsAsync(dbId);
            var wfIndexPolicy = new IndexingPolicy();
            wfIndexPolicy.IncludedPaths.Add(new IncludedPath { Path = @"/*" });
            wfIndexPolicy.ExcludedPaths.Add(new ExcludedPath { Path = @"/ExecutionPointers/?" });

            Task.WaitAll(
                dbResp.Database.CreateContainerIfNotExistsAsync(new ContainerProperties(CosmosDbPersistenceProvider.WorkflowContainerName, @"/id")
                {
                    IndexingPolicy = wfIndexPolicy
                }),
                dbResp.Database.CreateContainerIfNotExistsAsync(new ContainerProperties(CosmosDbPersistenceProvider.EventContainerName, @"/id")),
                dbResp.Database.CreateContainerIfNotExistsAsync(new ContainerProperties(CosmosDbPersistenceProvider.SubscriptionContainerName, @"/id"))
            );
        }

    }
}