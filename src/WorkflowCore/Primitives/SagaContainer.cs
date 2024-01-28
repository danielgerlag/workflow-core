#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Primitives
{
    public class SagaContainer<
#if NET8_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
        TStepBody> : WorkflowStep<TStepBody>
        where TStepBody : IStepBody
    {
        public override bool ResumeChildrenAfterCompensation => false;
        public override bool RevertChildrenAfterCompensation => true;

        public override void PrimeForRetry(ExecutionPointer pointer)
        {
            base.PrimeForRetry(pointer);
            pointer.PersistenceData = null;
        }
    }
}
