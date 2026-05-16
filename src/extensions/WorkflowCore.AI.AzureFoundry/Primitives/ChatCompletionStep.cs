using System;
using WorkflowCore.Models;

namespace WorkflowCore.AI.AzureFoundry.Primitives
{
    /// <summary>
    /// WorkflowStep wrapper for ChatCompletion
    /// </summary>
    public class ChatCompletionStep : WorkflowStep<ChatCompletion>
    {
        public override Type BodyType => typeof(ChatCompletion);
    }
}
