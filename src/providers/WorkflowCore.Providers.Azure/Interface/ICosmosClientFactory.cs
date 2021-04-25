using Microsoft.Azure.Cosmos;

namespace WorkflowCore.Providers.Azure.Interface
{
    public interface ICosmosClientFactory
    {
        CosmosClient GetCosmosClient();
    }
}
