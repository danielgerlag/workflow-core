using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Users.Primitives
{
    public class Escalate : StepBody
    {
        public TimeSpan TimeOut { get; set; }

        public string NewUser { get; set; }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            if (context.PersistenceData != null)
            {
                var taskStep = context.Workflow.ExecutionPointers.FindById(context.ExecutionPointer.PredecessorId);

                if (!taskStep.EventPublished)
                {
                    taskStep.ExtensionAttributes[UserTask.ExtAssignPrincipal] = NewUser;
                }
                return ExecutionResult.Next();
            }

            return ExecutionResult.Sleep(TimeOut, true);
        }
    }
}
