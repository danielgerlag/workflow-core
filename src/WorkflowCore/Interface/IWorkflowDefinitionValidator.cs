using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IWorkflowDefinitionValidator
    {
        bool IsDefinitionValid(WorkflowDefinition definition);
    }
}