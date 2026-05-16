using System;
using WorkflowCore.Models;

namespace WorkflowCore.AI.AzureFoundry.Primitives
{
    /// <summary>
    /// WorkflowStep wrapper for AgentLoop
    /// </summary>
    public class AgentLoopStep : WorkflowStep<AgentLoop>
    {
        public override Type BodyType => typeof(AgentLoop);
    }
}
