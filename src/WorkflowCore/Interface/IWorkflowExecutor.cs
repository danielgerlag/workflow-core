using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IWorkflowExecutor
    {
        WorkflowExecutorResult Execute(WorkflowInstance workflow, WorkflowOptions options);
    }
}