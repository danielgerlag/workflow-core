using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public class SubscriptionStep<TStepBody> : WorkflowStep<TStepBody>, ISubscriptionStep<TStepBody>
        where TStepBody : SubscriptionStepBody
    {
        public string EventKey { get; set; }

        public string EventName { get; set; }

        public DateTime EffectiveDateUTC { get; set; }

        public override ExecutionPipelineDirective InitForExecution(IWorkflowHost host, IPersistenceProvider persistenceStore, WorkflowDefinition defintion, WorkflowInstance workflow, ExecutionPointer executionPointer)
        {
            if (!executionPointer.EventPublished)
            {
                executionPointer.EventKey = EventKey;
                executionPointer.EventName = EventName;
                executionPointer.Active = false;
                persistenceStore.PersistWorkflow(workflow).Wait();
                host.SubscribeEvent(workflow.Id, executionPointer.StepId, executionPointer.EventName, executionPointer.EventKey, DateTime.Now.ToUniversalTime()).Wait();

                return ExecutionPipelineDirective.Defer;
            }
            return ExecutionPipelineDirective.Next;
        }

        public override ExecutionPipelineDirective BeforeExecute(IWorkflowHost host, IPersistenceProvider persistenceStore, IStepExecutionContext context, ExecutionPointer executionPointer, IStepBody body)
        {
            if (executionPointer.EventPublished)
            {
                if (body is ISubscriptionBody)
                    (body as ISubscriptionBody).EventData = executionPointer.EventData;
            }
            return ExecutionPipelineDirective.Next;
        }
    }
}
