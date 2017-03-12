using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Users.Models
{
    public class UserStepContainer : WorkflowStep<UserStep>        
    {
        public LambdaExpression Principal { get; set; }

        public string UserPrompt { get; set; }

        public override ExecutionPipelineDirective InitForExecution(IWorkflowHost host, IPersistenceProvider persistenceStore, WorkflowDefinition defintion, WorkflowInstance workflow, ExecutionPointer executionPointer)
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
                    userOptions[outcome.Label ?? Convert.ToString(outcome.Value ?? "Proceed")] = outcome.Value;
                }
                executionPointer.ExtensionAttributes["UserOptions"] = userOptions;

                executionPointer.EventKey = workflow.Id + "." + executionPointer.Id;
                executionPointer.EventName = "UserAction";
                executionPointer.Active = false;
                persistenceStore.PersistWorkflow(workflow).Wait();
                host.SubscribeEvent(workflow.Id, executionPointer.StepId, executionPointer.EventName, executionPointer.EventKey, DateTime.Now.ToUniversalTime());

                return ExecutionPipelineDirective.Defer;
            }
            return ExecutionPipelineDirective.Next;
        }

        public override ExecutionPipelineDirective BeforeExecute(IWorkflowHost host, IPersistenceProvider persistenceStore, IStepExecutionContext context, ExecutionPointer executionPointer, IStepBody body)
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
