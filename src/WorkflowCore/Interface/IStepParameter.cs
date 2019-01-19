using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IStepParameter
    {
        void AssignInput(object data, IStepBody body, IStepExecutionContext context);
        void AssignOutput(object data, IStepBody body, IStepExecutionContext context);
    }
}