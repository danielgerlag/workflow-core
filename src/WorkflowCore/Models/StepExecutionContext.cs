using WorkflowCore.Interface;

namespace WorkflowCore.Models
{
    public class StepExecutionContext : IStepExecutionContext
    {
        public WorkflowInstance Workflow { get; set; }

        public WorkflowStep Step { get; set; }

        public ExecutionPointer ExecutionPointer { get; set; }

        public object PersistenceData { get; set; }

        public object Item { get; set; }
    }
}
