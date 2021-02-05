using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    /// <summary>
    /// Default implementation of <see cref="IWorkflowMiddlewareErrorHandler"/>. Just logs the
    /// thrown exception and moves on.
    /// </summary>
    public class DefaultWorkflowMiddlewareErrorHandler : IWorkflowMiddlewareErrorHandler
    {
        private readonly ILogger<DefaultWorkflowMiddlewareErrorHandler> _log;

        public DefaultWorkflowMiddlewareErrorHandler(ILogger<DefaultWorkflowMiddlewareErrorHandler> log)
        {
            _log = log;
        }

        /// <summary>
        /// Asynchronously handle the given exception.
        /// </summary>
        /// <param name="workflowInstance">Workflow instance where error happened</param>
        /// <param name="ex">The exception to handle</param>
        /// <returns>A task that completes when handling is done.</returns>
        public Task HandleAsync(WorkflowInstance workflowInstance, Exception ex)
        {
            _log.LogError(ex, "An error occurred running workflow '{workflow}' middleware: {Message}", workflowInstance.Id, ex.Message);
            return Task.CompletedTask;
        }
    }
}
