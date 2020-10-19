using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    /// <summary>
    /// Determines at which point to run the middleware.
    /// </summary>
    public enum WorkflowMiddlewarePhase
    {
        /// <summary>
        /// The middleware should run before a workflow starts.
        /// </summary>
        PreWorkflow,

        /// <summary>
        /// The middleware should run after a workflow completes.
        /// </summary>
        PostWorkflow
    }

    /// <summary>
    /// Middleware that can run before a workflow starts or after a workflow completes.
    /// </summary>
    public interface IWorkflowMiddleware
    {
        /// <summary>
        /// The phase in the workflow execution to run this middleware in
        /// </summary>
        WorkflowMiddlewarePhase Phase { get; }

        /// <summary>
        /// Runs the middleware on the given <see cref="WorkflowInstance"/>.
        /// </summary>
        /// <param name="workflow">The <see cref="WorkflowInstance"/>.</param>
        /// <param name="next">The next middleware in the chain.</param>
        /// <returns>A <see cref="Task"/> that completes asynchronously once the
        /// middleware chain finishes running.</returns>
        Task HandleAsync(WorkflowInstance workflow, WorkflowDelegate next);
    }
}
