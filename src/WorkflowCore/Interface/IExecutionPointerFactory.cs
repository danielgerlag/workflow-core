using System;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IExecutionPointerFactory
    {
        ExecutionPointer BuildGenesisPointer(WorkflowDefinition def);
        ExecutionPointer BuildCompensationPointer(WorkflowDefinition def, ExecutionPointer pointer, ExecutionPointer exceptionPointer, int compensationStepId);
        ExecutionPointer BuildCatchPointer(WorkflowDefinition def, ExecutionPointer pointer, ExecutionPointer exceptionPointer, int catchStepId, Exception exception);
        ExecutionPointer BuildNextPointer(WorkflowDefinition def, ExecutionPointer pointer, IStepOutcome outcomeTarget);
        ExecutionPointer BuildChildPointer(WorkflowDefinition def, ExecutionPointer pointer, int childDefinitionId, object branch);
    }
}