using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IStepBody
    {        
        Task<ExecutionResult> RunAsync(IStepExecutionContext context);        
    }
}
