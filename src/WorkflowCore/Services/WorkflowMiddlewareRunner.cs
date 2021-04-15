using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    /// <inheritdoc cref="IWorkflowMiddlewareRunner" />
    public class WorkflowMiddlewareRunner : IWorkflowMiddlewareRunner
    {
        private static readonly WorkflowDelegate NoopWorkflowDelegate = () => Task.CompletedTask;
        private readonly IEnumerable<IWorkflowMiddleware> _middleware;
        private readonly IServiceProvider _serviceProvider;

        public WorkflowMiddlewareRunner(
            IEnumerable<IWorkflowMiddleware> middleware,
            IServiceProvider serviceProvider)
        {
            _middleware = middleware;
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc cref="IWorkflowMiddlewareRunner.RunPreMiddleware"/>
        public async Task RunPreMiddleware(WorkflowInstance workflow, WorkflowDefinition def)
        {
            var preMiddleware = _middleware
                .Where(m => m.Phase == WorkflowMiddlewarePhase.PreWorkflow);

            await RunWorkflowMiddleware(workflow, preMiddleware);
        }

        /// <inheritdoc cref="IWorkflowMiddlewareRunner.RunPostMiddleware"/>
        public async Task RunPostMiddleware(WorkflowInstance workflow, WorkflowDefinition def)
        {
            var postMiddleware = _middleware
                .Where(m => m.Phase == WorkflowMiddlewarePhase.PostWorkflow);
            try
            {
                await RunWorkflowMiddleware(workflow, postMiddleware);
            }
            catch (Exception exception)
            {
                await HandleWorkflowMiddlewareError(def.OnPostMiddlewareError, exception);
            }
        }

        /// <inheritdoc cref="IWorkflowMiddlewareRunner.RunExecuteMiddleware"/>
        public async Task RunExecuteMiddleware(WorkflowInstance workflow, WorkflowDefinition def)
        {
            var executeMiddleware = _middleware
                .Where(m => m.Phase == WorkflowMiddlewarePhase.ExecuteWorkflow);

            try
            {
                await RunWorkflowMiddleware(workflow, executeMiddleware);
            }
            catch (Exception exception)
            {
                await HandleWorkflowMiddlewareError(def.OnExecuteMiddlewareError, exception);
            }
        }

        private static Task RunWorkflowMiddleware(
            WorkflowInstance workflow,
            IEnumerable<IWorkflowMiddleware> middlewareCollection)
        {
            return middlewareCollection
                .Reverse()
                .Aggregate(NoopWorkflowDelegate,
                    (previous, middleware) =>
                        () => middleware.HandleAsync(workflow, previous))();
        }

        private async Task HandleWorkflowMiddlewareError(Type middlewareErrorType, Exception exception)
        {
            var errorHandlerType = middlewareErrorType ?? typeof(IWorkflowMiddlewareErrorHandler);

            using (var scope = _serviceProvider.CreateScope())
            {
                var typeInstance = scope.ServiceProvider.GetService(errorHandlerType);
                if (typeInstance is IWorkflowMiddlewareErrorHandler handler)
                {
                    await handler.HandleAsync(exception);
                }
            }
        }
    }
}
