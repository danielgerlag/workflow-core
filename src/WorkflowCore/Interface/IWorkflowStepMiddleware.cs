using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    /// <summary>
    /// Middleware that runs around a workflow step and can enhance or alter
    /// the steps behavior.
    /// </summary>
    public interface IWorkflowStepMiddleware
    {
        /// <summary>
        /// Handle the workflow step and return an <see cref="ExecutionResult"/>
        /// asynchronously. It is important to invoke <see cref="next"/> at some point
        /// in the middleware. Not doing so will prevent the workflow step from ever
        /// getting executed.
        /// </summary>
        /// <param name="context">The step's context.</param>
        /// <param name="body">An instance of the step body that is going to be run.</param>
        /// <param name="next">The next middleware in the chain.</param>
        /// <returns>A <see cref="Task{ExecutionResult}"/> of the workflow result.</returns>
        Task<ExecutionResult> HandleAsync(
            IStepExecutionContext context,
            IStepBody body,
            WorkflowStepDelegate next
        );
    }
}
