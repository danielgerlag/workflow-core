using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    /// <summary>
    /// Runs workflow pre/post middleware.
    /// </summary>
    public interface IWorkflowMiddlewareRunner
    {
        /// <summary>
        /// Runs workflow-level middleware that is set to run at the
        /// <see cref="WorkflowMiddlewarePhase.PreWorkflow"/> phase. Middleware will be run in the
        /// order in which they were registered with DI with middleware declared earlier starting earlier and
        /// completing later.
        /// </summary>
        /// <param name="workflow">The <see cref="WorkflowInstance"/> to run for.</param>
        /// <param name="def">The <see cref="WorkflowDefinition"/> definition.</param>
        /// <returns>A task that will complete when all middleware has run.</returns>
        Task RunPreMiddleware(WorkflowInstance workflow, WorkflowDefinition def);

        /// <summary>
        /// Runs workflow-level middleware that is set to run at the
        /// <see cref="WorkflowMiddlewarePhase.PostWorkflow"/> phase. Middleware will be run in the
        /// order in which they were registered with DI with middleware declared earlier starting earlier and
        /// completing later.
        /// </summary>
        /// <param name="workflow">The <see cref="WorkflowInstance"/> to run for.</param>
        /// <param name="def">The <see cref="WorkflowDefinition"/> definition.</param>
        /// <returns>A task that will complete when all middleware has run.</returns>
        Task RunPostMiddleware(WorkflowInstance workflow, WorkflowDefinition def);
    }
}
