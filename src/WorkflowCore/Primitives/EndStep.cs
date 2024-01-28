using System;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public class EndStep : WorkflowStep
    {
#if NET8_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
        public override Type BodyType => null;

        public override ExecutionPipelineDirective InitForExecution(
            WorkflowExecutorResult executorResult,
            WorkflowDefinition definition,
            WorkflowInstance workflow,
            ExecutionPointer executionPointer)
        {
            return ExecutionPipelineDirective.EndWorkflow;
        }
    }
}
