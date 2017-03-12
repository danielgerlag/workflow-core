using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public class SubscriptionStep<TStepBody> : WorkflowStep<TStepBody>, ISubscriptionStep<TStepBody>
        where TStepBody : SubscriptionStepBody
    {
        public LambdaExpression EventKey { get; set; }

        public string EventName { get; set; }

        public LambdaExpression EffectiveDate { get; set; }

        public override ExecutionPipelineDirective InitForExecution(IWorkflowHost host, IPersistenceProvider persistenceStore, WorkflowDefinition defintion, WorkflowInstance workflow, ExecutionPointer executionPointer)
        {
            if (!executionPointer.EventPublished)
            {
                if (EventKey != null)
                    executionPointer.EventKey = Convert.ToString(EventKey.Compile().DynamicInvoke(workflow.Data));

                DateTime effectiveDate = DateTime.Now.ToUniversalTime();

                if (EffectiveDate != null)
                    effectiveDate = Convert.ToDateTime(EffectiveDate.Compile().DynamicInvoke(workflow.Data));

                executionPointer.EventName = EventName;
                executionPointer.Active = false;
                persistenceStore.PersistWorkflow(workflow).Wait();
                host.SubscribeEvent(workflow.Id, executionPointer.StepId, executionPointer.EventName, executionPointer.EventKey, effectiveDate).Wait();

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
