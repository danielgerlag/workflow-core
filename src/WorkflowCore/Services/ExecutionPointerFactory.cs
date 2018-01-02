using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class ExecutionPointerFactory : IExecutionPointerFactory
    {

        public ExecutionPointer BuildStartingPointer(WorkflowDefinition def)
        {
            return new ExecutionPointer
            {
                Id = GenerateId(),
                StepId = 0,
                Active = true,
                Status = PointerStatus.Pending,
                StepName = Enumerable.First<WorkflowStep>(def.Steps, x => x.Id == 0).Name
            };
        }

        public ExecutionPointer BuildNextPointer(WorkflowDefinition def, ExecutionPointer pointer, StepOutcome outcomeTarget)
        {
            var nextId = GenerateId();
            return new ExecutionPointer()
            {
                Id = nextId,
                PredecessorId = pointer.Id,
                StepId = outcomeTarget.NextStep,
                Active = true,
                ContextItem = pointer.ContextItem,
                Status = PointerStatus.Pending,
                StepName = def.Steps.First(x => x.Id == outcomeTarget.NextStep).Name,
                Scope = new Stack<string>(pointer.Scope)
            };            
        }

        public ExecutionPointer BuildChildPointer(WorkflowDefinition def, ExecutionPointer pointer, int childDefinitionId, object branch)
        {
            var childPointerId = GenerateId();
            var childScope = new Stack<string>(pointer.Scope);
            childScope.Push(pointer.Id);
            pointer.Children.Add(childPointerId);

            return new ExecutionPointer()
            {
                Id = childPointerId,
                PredecessorId = pointer.Id,
                StepId = childDefinitionId,
                Active = true,
                ContextItem = branch,
                Status = PointerStatus.Pending,
                StepName = def.Steps.First(x => x.Id == childDefinitionId).Name,
                Scope = childScope
            };            
        }

        public ExecutionPointer BuildCompensationPointer(WorkflowDefinition def, ExecutionPointer pointer, ExecutionPointer exceptionPointer, int compensationStepId)
        {
            var nextId = GenerateId();
            return new ExecutionPointer()
            {
                Id = nextId,
                PredecessorId = exceptionPointer.Id,
                StepId = compensationStepId,
                Active = true,
                ContextItem = pointer.ContextItem,
                Status = PointerStatus.Pending,
                StepName = def.Steps.First(x => x.Id == compensationStepId).Name,
                Scope = new Stack<string>(pointer.Scope)
            };
        }

        private string GenerateId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
