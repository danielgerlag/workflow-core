using System.Threading;
using WorkflowCore.Interface;

namespace WorkflowCore.Services
{
    public class StepExecutionContextAccessor : IStepExecutionContextAccessor
    {
        private static readonly AsyncLocal<IStepExecutionContext> _contextCurrent = new AsyncLocal<IStepExecutionContext>();

        public IStepExecutionContext StepExecutionContext
        {
            get => _contextCurrent.Value;
            set => _contextCurrent.Value = value;
        }
    }
}
