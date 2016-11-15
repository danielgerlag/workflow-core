
using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IStepExecutionContext
    {
        object PersistenceData { get; set; }
        WorkflowStep Step { get; set; }
        WorkflowInstance Workflow { get; set; }
    }
}