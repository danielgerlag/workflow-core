using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Users.Models
{
    [Obsolete]
    public class UserStepContainer : WorkflowStep<UserStep>        
    {
        public LambdaExpression Principal { get; set; }

        public string UserPrompt { get; set; }

        public override ExecutionPipelineDirective InitForExecution(WorkflowExecutorResult executorResult, WorkflowDefinition defintion, WorkflowInstance workflow, ExecutionPointer executionPointer)
        {
            if (!executionPointer.EventPublished)
            {
                //resolve principal to be assigned
                var resolvedUser = Principal.Compile().DynamicInvoke(workflow.Data);

                executionPointer.ExtensionAttributes["AssignedPrincipal"] = resolvedUser;
                executionPointer.ExtensionAttributes["Prompt"] = UserPrompt;

                Dictionary<string, object> userOptions = new Dictionary<string, object>();
                foreach (var outcome in Outcomes)
                {
                    if (outcome is ValueOutcome)
                    {
                        userOptions[outcome.Label ?? Convert.ToString((outcome as ValueOutcome).GetValue(workflow.Data) ?? "Proceed")] = (outcome as ValueOutcome).GetValue(workflow.Data);
                    }
                }
                executionPointer.ExtensionAttributes["UserOptions"] = userOptions;

                executionPointer.EventKey = workflow.Id + "." + executionPointer.Id;
                executionPointer.EventName = "UserAction";
                executionPointer.Active = false;

                executorResult.Subscriptions.Add(new EventSubscription
                {
                    WorkflowId = workflow.Id,
                    StepId = executionPointer.StepId,
                    EventName = executionPointer.EventName,
                    EventKey = executionPointer.EventKey,
                    SubscribeAsOf = DateTime.Now.ToUniversalTime()
                });

                return ExecutionPipelineDirective.Defer;
            }
            return ExecutionPipelineDirective.Next;
        }

        public override ExecutionPipelineDirective BeforeExecute(WorkflowExecutorResult executorResult, IStepExecutionContext context, ExecutionPointer executionPointer, IStepBody body)
        {
            if (executionPointer.EventPublished)
            {
                if ((body is UserStep) && (executionPointer.EventData is UserAction))
                {
                    (body as UserStep).UserAction = (executionPointer.EventData as UserAction);
                    executionPointer.ExtensionAttributes["ActionUser"] = (executionPointer.EventData as UserAction).User;
                }
            }
            return ExecutionPipelineDirective.Next;
        }
    }
}
