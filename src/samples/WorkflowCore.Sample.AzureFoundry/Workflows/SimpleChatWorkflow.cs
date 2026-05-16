using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Primitives;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Sample.AzureFoundry.Workflows
{
    /// <summary>
    /// Simple workflow demonstrating basic chat completion
    /// </summary>
    public class SimpleChatWorkflow : IWorkflow<ChatWorkflowData>
    {
        public string Id => "SimpleChatWorkflow";
        public int Version => 1;

        public void Build(IWorkflowBuilder<ChatWorkflowData> builder)
        {
            builder
                .StartWith(context => ExecutionResult.Next())
                .ChatCompletion(cfg => cfg
                    .SystemPrompt("You are a helpful, friendly assistant. Keep responses concise.")
                    .UserMessage(data => data.UserMessage)
                    .Temperature(0.7f)
                    .MaxTokens(500)
                    .OutputTo(data => data.Response)
                    .OutputTokensTo(data => data.TokensUsed));
        }
    }
}
