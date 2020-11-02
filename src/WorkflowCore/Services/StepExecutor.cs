using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    /// <summary>
    /// Executes the workflow step and applies any <see cref="IWorkflowStepMiddleware"/> to the step.
    /// </summary>
    public class StepExecutor : IStepExecutor
    {
        private readonly IEnumerable<IWorkflowStepMiddleware> _stepMiddleware;

        public StepExecutor(
            IEnumerable<IWorkflowStepMiddleware> stepMiddleware
        )
        {
            _stepMiddleware = stepMiddleware;
        }

        /// <summary>
        /// Runs the passed <see cref="IStepBody"/> in the given <see cref="IStepExecutionContext"/> while applying
        /// any <see cref="IWorkflowStepMiddleware"/> registered in the system. Middleware will be run in the
        /// order in which they were registered with DI with middleware declared earlier starting earlier and
        /// completing later.
        /// </summary>
        /// <param name="context">The <see cref="IStepExecutionContext"/> in which to execute the step.</param>
        /// <param name="body">The <see cref="IStepBody"/> body.</param>
        /// <returns>A <see cref="Task{ExecutionResult}"/> to wait for the result of running the step</returns>
        public async Task<ExecutionResult> ExecuteStep(
            IStepExecutionContext context,
            IStepBody body
        )
        {
            // Build the middleware chain by reducing over all the middleware in reverse starting with step body
            // and building step delegates that call out to the next delegate in the chain
            Task<ExecutionResult> Step() => body.RunAsync(context);
            var middlewareChain = _stepMiddleware
                .Reverse()
                .Aggregate(
                    (WorkflowStepDelegate) Step,
                    (previous, middleware) => () => middleware.HandleAsync(context, body, previous)
                );

            // Run the middleware chain
            return await middlewareChain();
        }
    }
}
