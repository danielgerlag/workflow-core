using System;
using WorkflowCore.Models;

namespace WorkflowCore.AI.AzureFoundry.Primitives
{
    /// <summary>
    /// WorkflowStep wrapper for VectorSearch
    /// </summary>
    public class VectorSearchStep : WorkflowStep<VectorSearch>
    {
        public override Type BodyType => typeof(VectorSearch);
    }
}
