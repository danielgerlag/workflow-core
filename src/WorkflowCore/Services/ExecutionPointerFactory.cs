using System;
using System.Collections.Generic;
using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class ExecutionPointerFactory : IExecutionPointerFactory
    {
        public ExecutionPointer BuildGenesisPointer(WorkflowDefinition def)
        {
            return new ExecutionPointer
            {
                Id = GenerateId(),
                StepId = 0,
                Active = true,
                Status = PointerStatus.Pending,
                StepName = def.Steps.FindById(0).Name
            };
        }

        public ExecutionPointer BuildNextPointer(WorkflowDefinition def, ExecutionPointer pointer, IStepOutcome outcomeTarget)
        {
            var nextId = GenerateId();
            return new ExecutionPointer
            {
                Id = nextId,
                PredecessorId = pointer.Id,
                StepId = outcomeTarget.NextStep,
                Active = true,
                ContextItem = pointer.ContextItem,
                Status = PointerStatus.Pending,
                StepName = def.Steps.FindById(outcomeTarget.NextStep).Name,
                Scope = new List<string>(pointer.Scope)
            };            
        }

        public ExecutionPointer BuildChildPointer(WorkflowDefinition def, ExecutionPointer pointer, int childDefinitionId, object branch)
        {
            var childPointerId = GenerateId();
            var childScope = new List<string>(pointer.Scope);
            childScope.Insert(0, pointer.Id);
            pointer.Children.Add(childPointerId);

            return new ExecutionPointer
            {
                Id = childPointerId,
                PredecessorId = pointer.Id,
                StepId = childDefinitionId,
                Active = true,
                ContextItem = branch,
                Status = PointerStatus.Pending,
                StepName = def.Steps.FindById(childDefinitionId).Name,
                Scope = new List<string>(childScope)
            };            
        }

        public ExecutionPointer BuildCompensationPointer(WorkflowDefinition def, ExecutionPointer pointer, ExecutionPointer exceptionPointer, int compensationStepId)
        {
            var nextId = GenerateId();
            return new ExecutionPointer
            {
                Id = nextId,
                PredecessorId = exceptionPointer.Id,
                StepId = compensationStepId,
                Active = true,
                ContextItem = pointer.ContextItem,
                Status = PointerStatus.Pending,
                StepName = def.Steps.FindById(compensationStepId).Name,
                Scope = new List<string>(pointer.Scope)
            };
        }

        private string GenerateId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
