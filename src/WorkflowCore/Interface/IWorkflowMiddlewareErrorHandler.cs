using System;
using System.Threading.Tasks;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    /// <summary>
    /// Handles exceptions within workflow middleware.
    /// </summary>
    public interface IWorkflowMiddlewareErrorHandler
    {
        /// <summary>
        /// Asynchronously handle the given exception.
        /// </summary>
        /// <param name="workflowInstance">Workflow instance where error happened</param>
        /// <param name="ex">The exception to handle</param>
        /// <returns>A task that completes when handling is done.</returns>
        Task HandleAsync(WorkflowInstance workflowInstance, Exception ex);
    }
}
