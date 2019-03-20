using System.Threading.Tasks;

namespace WorkflowCore.Providers.AWS.Interface
{
    public interface IDynamoDbProvisioner
    {
        Task ProvisionTables();
    }
}