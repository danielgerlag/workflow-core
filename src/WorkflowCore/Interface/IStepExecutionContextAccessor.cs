namespace WorkflowCore.Interface
{
    public interface IStepExecutionContextAccessor
    {
        IStepExecutionContext StepExecutionContext { get; }
    }
}
