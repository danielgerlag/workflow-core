using Microsoft.Azure.Cosmos;

namespace WorkflowCore.Providers.Azure.Interface
{
    public interface ICosmosDbClient
    {
        CosmosClient GetCosmosClient();
    }
}
