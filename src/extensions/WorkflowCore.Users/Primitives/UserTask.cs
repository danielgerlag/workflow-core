using System;
using System.Collections.Generic;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Primitives;
using WorkflowCore.Users.Models;

namespace WorkflowCore.Users.Primitives
{
    public class UserTask : ContainerStepBody
    {
        public string AssignedPrincipal { get; set; }

        public string Prompt { get; set; }

        public const string EventName = "UserAction";
        public const string ExtAssignPrincipal = "AssignedPrincipal";
        public const string ExtPrompt = "Prompt";
        public const string ExtUserOptions = "UserOptions";
        private readonly Dictionary<string, string> _options;
        private readonly IEnumerable<EscalateStep> _escalations;

        public UserTask(Dictionary<string, string> options, IEnumerable<EscalateStep> escalations)
        {
            _options = options;
            _escalations = escalations;
        }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            if (!context.ExecutionPointer.EventPublished)
            {
                context.ExecutionPointer.ExtensionAttributes[ExtAssignPrincipal] = AssignedPrincipal;
                context.ExecutionPointer.ExtensionAttributes[ExtPrompt] = Prompt;
                context.ExecutionPointer.ExtensionAttributes[ExtUserOptions] = _options;

                var effectiveDate = DateTime.Now.ToUniversalTime();
                var eventKey = context.Workflow.Id + "." + context.ExecutionPointer.Id;

                SetupEscalations(context);

                return ExecutionResult.WaitForEvent(EventName, eventKey, effectiveDate);
            }

            if (!(context.ExecutionPointer.EventData is UserAction))
                throw new ArgumentException();
            
            var action = ((UserAction) context.ExecutionPointer.EventData);
            context.ExecutionPointer.ExtensionAttributes["ActionUser"] = action.User;
            
            if (context.PersistenceData == null)
            {
                var result = ExecutionResult.Branch(new List<object>() { context.Item }, new ControlPersistenceData() { ChildrenActive = true });
                result.OutcomeValue = action.OutcomeValue;
                return result;
            }

            if ((context.PersistenceData is ControlPersistenceData) && ((context.PersistenceData as ControlPersistenceData).ChildrenActive))
            {
                if (context.Workflow.IsBranchComplete(context.ExecutionPointer.Id))
                    return ExecutionResult.Next();
                else
                {
                    var result = ExecutionResult.Persist(context.PersistenceData);
                    result.OutcomeValue = action.OutcomeValue;
                    return result;
                }
            }

            throw new ArgumentException("PersistenceData");
        }

        private void SetupEscalations(IStepExecutionContext context)
        {
            foreach (var esc in _escalations)
            {
                context.Workflow.ExecutionPointers.Add(new ExecutionPointer()
                {
                    Active = true,
                    Id = Guid.NewGuid().ToString(),
                    PredecessorId = context.ExecutionPointer.Id,
                    StepId = esc.Id,
                    StepName = esc.Name,
                    Status = PointerStatus.Pending,
                    Scope = new List<string>(context.ExecutionPointer.Scope)
                });
            }
        }
    }
}
