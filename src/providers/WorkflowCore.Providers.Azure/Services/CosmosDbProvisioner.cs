using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace WorkflowCore.Providers.Azure.Services
{
    public class CosmosDbProvisioner
    {

        private CosmosClient _client;
        
        public CosmosDbProvisioner()
        {
            
        }

        public async Task Provision(string dbId)
        {
            var dbResp = await _client.CreateDatabaseIfNotExistsAsync(dbId);
            var wfContainer = await dbResp.Database.CreateContainerIfNotExistsAsync(new ContainerProperties("workflows", "id"));
            
        }
        
        
    }
}