using System.Threading;
using System.Threading.Tasks;

namespace WorkflowCore.Providers.Azure.Interface
{
    public interface ICosmosDbProvisioner
    {
        Task Provision(string dbId, CancellationToken cancellationToken = default);
    }
}