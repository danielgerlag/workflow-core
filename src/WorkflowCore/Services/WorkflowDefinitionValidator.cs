using System.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class WorkflowDefinitionValidator : IWorkflowDefinitionValidator
    {
        public bool IsDefinitionValid(WorkflowDefinition definition)
        {
            return definition.Steps.Count(x => x.ErrorBehavior == WorkflowErrorHandling.Catch &&
                                               x.CatchStepsQueue.Count == 0) == 0;
        }
    }
}