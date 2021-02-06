using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    /// <inheritdoc />
    public class WorkflowMiddlewareRunner : IWorkflowMiddlewareRunner
    {
        private static readonly WorkflowDelegate NoopWorkflowDelegate = () => Task.CompletedTask;
        private readonly IEnumerable<IWorkflowMiddleware> _middleware;
        private readonly IServiceProvider _serviceProvider;

        public WorkflowMiddlewareRunner(
            IEnumerable<IWorkflowMiddleware> middleware,
            IServiceProvider serviceProvider
        )
        {
            _middleware = middleware;
            _serviceProvider = serviceProvider;
        }


        /// <summary>
        /// Runs workflow-level middleware that is set to run at the
        /// <see cref="WorkflowMiddlewarePhase.PreWorkflow"/> phase. Middleware will be run in the
        /// order in which they were registered with DI with middleware declared earlier starting earlier and
        /// completing later.
        /// </summary>
        /// <param name="workflow">The <see cref="WorkflowInstance"/> to run for.</param>
        /// <param name="def">The <see cref="WorkflowDefinition"/> definition.</param>
        /// <returns>A task that will complete when all middleware has run.</returns>
        public async Task RunPreMiddleware(WorkflowInstance workflow, WorkflowDefinition def)
        {
            var preMiddleware = _middleware
                .Where(m => m.Phase == WorkflowMiddlewarePhase.PreWorkflow)
                .ToArray();

            await RunWorkflowMiddleware(workflow, preMiddleware);
        }

        /// <summary>
        /// Runs workflow-level middleware that is set to run at the
        /// <see cref="WorkflowMiddlewarePhase.PostWorkflow"/> phase. Middleware will be run in the
        /// order in which they were registered with DI with middleware declared earlier starting earlier and
        /// completing later.
        /// </summary>
        /// <param name="workflow">The <see cref="WorkflowInstance"/> to run for.</param>
        /// <param name="def">The <see cref="WorkflowDefinition"/> definition.</param>
        /// <returns>A task that will complete when all middleware has run.</returns>
        public async Task RunPostMiddleware(WorkflowInstance workflow, WorkflowDefinition def)
        {
            var postMiddleware = _middleware
                .Where(m => m.Phase == WorkflowMiddlewarePhase.PostWorkflow)
                .ToArray();

            try
            {
                await RunWorkflowMiddleware(workflow, postMiddleware);
            }
            catch (Exception exception)
            {
                // On error, determine which error handler to run and then run it
                var errorHandlerType = def.OnPostMiddlewareError ?? typeof(IWorkflowMiddlewareErrorHandler);
                using (var scope = _serviceProvider.CreateScope())
                {
                    var typeInstance = scope.ServiceProvider.GetService(errorHandlerType);
                    if (typeInstance != null && typeInstance is IWorkflowMiddlewareErrorHandler handler)
                    {
                        await handler.HandleAsync(workflow, exception);
                    }
                }
            }
        }

        private static async Task RunWorkflowMiddleware(
            WorkflowInstance workflow,
            IEnumerable<IWorkflowMiddleware> middlewareCollection
        )
        {
            // Build the middleware chain
            var middlewareChain = middlewareCollection
                .Reverse()
                .Aggregate(
                    NoopWorkflowDelegate,
                    (previous, middleware) => () => middleware.HandleAsync(workflow, previous)
                );

            await middlewareChain();
        }
    }
}
