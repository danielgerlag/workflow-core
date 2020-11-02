using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    /// <summary>
    /// Executes a workflow step.
    /// </summary>
    public interface IStepExecutor
    {
        /// <summary>
        /// Runs the passed <see cref="IStepBody"/> in the given <see cref="IStepExecutionContext"/>.
        /// </summary>
        /// <param name="context">The <see cref="IStepExecutionContext"/> in which to execute the step.</param>
        /// <param name="body">The <see cref="IStepBody"/> body.</param>
        /// <returns>A <see cref="Task{ExecutionResult}"/> to wait for the result of running the step</returns>
        Task<ExecutionResult> ExecuteStep(
            IStepExecutionContext context,
            IStepBody body
        );
    }
}
