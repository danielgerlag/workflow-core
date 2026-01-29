using System;
using WorkflowCore.Models;

namespace WorkflowCore.AI.AzureFoundry.Primitives
{
    /// <summary>
    /// WorkflowStep wrapper for GenerateEmbedding
    /// </summary>
    public class GenerateEmbeddingStep : WorkflowStep<GenerateEmbedding>
    {
        public override Type BodyType => typeof(GenerateEmbedding);
    }
}
