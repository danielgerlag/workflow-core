using System;
using WorkflowCore.Models;

namespace WorkflowCore.AI.AzureFoundry.Primitives
{
    /// <summary>
    /// WorkflowStep wrapper for ExecuteTool
    /// </summary>
    public class ExecuteToolStep : WorkflowStep<ExecuteTool>
    {
        public override Type BodyType => typeof(ExecuteTool);
    }
}
