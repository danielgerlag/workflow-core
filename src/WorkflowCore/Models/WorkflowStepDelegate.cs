using System.Threading.Tasks;

namespace WorkflowCore.Models
{
    /// <summary>
    /// Represents a function that executes a workflow step and returns a result.
    /// </summary>
    public delegate Task<ExecutionResult> WorkflowStepDelegate();
}
