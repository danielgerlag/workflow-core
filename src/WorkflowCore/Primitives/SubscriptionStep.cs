using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public class SubscriptionStep<TStepBody> : WorkflowStep<TStepBody>, ISubscriptionStep<TStepBody>
        where TStepBody : SubscriptionStepBody
    {
        public LambdaExpression EventKey { get; set; }

        public string EventName { get; set; }

        public LambdaExpression EffectiveDate { get; set; }

        public override ExecutionPipelineDirective InitForExecution(WorkflowExecutorResult executorResult, WorkflowDefinition defintion, WorkflowInstance workflow, ExecutionPointer executionPointer)
        {
            if (!executionPointer.EventPublished)
            {
                if (EventKey != null)
                    executionPointer.EventKey = Convert.ToString(EventKey.Compile().DynamicInvoke(workflow.Data));

                DateTime effectiveDate = DateTime.MinValue;

                if (EffectiveDate != null)
                    effectiveDate = Convert.ToDateTime(EffectiveDate.Compile().DynamicInvoke(workflow.Data));

                executionPointer.EventName = EventName;
                executionPointer.Active = false;

                executorResult.Subscriptions.Add(new EventSubscription()
                {
                    WorkflowId = workflow.Id,
                    StepId = executionPointer.StepId,
                    EventName = executionPointer.EventName,
                    EventKey = executionPointer.EventKey,
                    SubscribeAsOf = effectiveDate
                });

                return ExecutionPipelineDirective.Defer;
            }
            return ExecutionPipelineDirective.Next;
        }

        public override ExecutionPipelineDirective BeforeExecute(WorkflowExecutorResult executorResult, IStepExecutionContext context, ExecutionPointer executionPointer, IStepBody body)
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
