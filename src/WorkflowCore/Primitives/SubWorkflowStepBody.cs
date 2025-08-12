using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;

namespace WorkflowCore.Primitives
{
    public class SubWorkflowStepBody : StepBody
    {
        private readonly IScopeProvider _scopeProvider;

        public SubWorkflowStepBody(IScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            var scope = _scopeProvider.CreateScope(context);
            var workflowController = scope.ServiceProvider.GetRequiredService<IWorkflowController>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(
                typeof(SubWorkflowStepBody).Namespace + "." + nameof(SubWorkflowStepBody));
            
            if (!context.ExecutionPointer.EventPublished)
            {
                var result = workflowController.StartWorkflow(SubWorkflowId, context.Workflow.Data, context.Workflow.Id).Result;
            
                logger.LogDebug("Started sub workflow {Name} with id='{SubId}' from workflow {WorkflowDefinitionId} ({Id})", 
                    SubWorkflowId, result, context.Workflow.WorkflowDefinitionId, context.Workflow.Id);

                logger.LogDebug("Workflow {Name} ({SubId}) is waiting for event SubWorkflowLifeCycleEvent with key='{EventKey}'", 
                    SubWorkflowId, result, result);

                var effectiveDate = DateTime.MinValue;
                return ExecutionResult.WaitForEvent(nameof(SubWorkflowLifeCycleEvent), result, effectiveDate);
            }
            
            logger.LogDebug("Sub workflow {Name} ({SubId}) completed", SubWorkflowId, 
                context.ExecutionPointer.EventKey);

            var persistenceProvider = scope.ServiceProvider.GetRequiredService<IPersistenceProvider>();
            var workflowInstance = persistenceProvider.GetWorkflowInstance(context.ExecutionPointer.EventKey).Result;
            if (workflowInstance.Status == WorkflowStatus.Terminated)
            {
                throw new NotImplementedException(workflowInstance.Status.ToString());    
            }
            
            Result = workflowInstance.Data;
            return ExecutionResult.Next();
        }

        public string SubWorkflowId { get; set; }

        public object Parameters { get; set; }

        public object Result { get; set; }
    }
}
