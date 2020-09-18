using System.Threading.Tasks;

namespace WorkflowCore.Providers.Azure.Interface
{
    public interface ICosmosDbProvisioner
    {
        Task Provision(string dbId);
    }
}