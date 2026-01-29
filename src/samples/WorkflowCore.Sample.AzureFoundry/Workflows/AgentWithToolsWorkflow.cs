using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Primitives;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Sample.AzureFoundry.Tools;

namespace WorkflowCore.Sample.AzureFoundry.Workflows
{
    /// <summary>
    /// Workflow demonstrating an agentic loop with tool execution
    /// </summary>
    public class AgentWithToolsWorkflow : IWorkflow<AgentWorkflowData>
    {
        public string Id => "AgentWithToolsWorkflow";
        public int Version => 1;

        public void Build(IWorkflowBuilder<AgentWorkflowData> builder)
        {
            builder
                .StartWith(context => ExecutionResult.Next())
                .AgentLoop(cfg => cfg
                    .SystemPrompt(@"You are a helpful assistant with access to tools.
Available tools:
- weather: Get current weather for a city
- calculator: Perform mathematical calculations

Use the tools when needed to answer user questions accurately.
Always provide a final answer after using tools.")
                    .Message(data => data.UserRequest)
                    .WithTool("weather")
                    .WithTool("calculator")
                    .MaxIterations(5)
                    .AutoExecuteTools(true)
                    .OutputTo(data => data.AgentResponse));
        }
    }
}
