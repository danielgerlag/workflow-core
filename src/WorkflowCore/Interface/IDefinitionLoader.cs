using WorkflowCore.Models;

namespace WorkflowCore.Interface
{
    public interface IDefinitionLoader
    {
        WorkflowDefinition LoadDefinition(string json);
    }
}