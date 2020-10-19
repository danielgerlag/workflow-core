using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;

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
        /// <param name="ex">The exception to handle</param>
        /// <returns>A task that completes when handling is done.</returns>
        public Task HandleAsync(Exception ex)
        {
            _log.LogError(ex, "An error occurred running workflow middleware: {Message}", ex.Message);
            return Task.CompletedTask;
        }
    }
}
