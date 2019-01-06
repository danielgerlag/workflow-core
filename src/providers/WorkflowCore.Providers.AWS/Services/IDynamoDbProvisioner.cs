using System.Threading.Tasks;

namespace WorkflowCore.Providers.AWS.Services
{
    public interface IDynamoDbProvisioner
    {
        Task ProvisionTables();
    }
}