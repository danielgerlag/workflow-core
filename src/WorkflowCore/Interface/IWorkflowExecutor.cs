using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IWorkflowExecutor
    {
        Task Execute(WorkflowInstance workflow, IPersistenceProvider persistenceStore, WorkflowOptions options);
    }
}