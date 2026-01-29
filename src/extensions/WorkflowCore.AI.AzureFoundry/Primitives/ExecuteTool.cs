using System;
using System.Threading.Tasks;
using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Models;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.AI.AzureFoundry.Primitives
{
    /// <summary>
    /// Step body for executing a single tool
    /// </summary>
    public class ExecuteTool : StepBodyAsync
    {
        private readonly IToolRegistry _toolRegistry;

        public ExecuteTool(IToolRegistry toolRegistry)
        {
            _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        }

        /// <summary>
        /// Name of the tool to execute
        /// </summary>
        public string ToolName { get; set; }

        /// <summary>
        /// Tool call ID (for correlating with LLM tool calls)
        /// </summary>
        public string ToolCallId { get; set; }

        /// <summary>
        /// JSON arguments for the tool
        /// </summary>
        public string Arguments { get; set; }

        // Outputs

        /// <summary>
        /// Result from the tool execution
        /// </summary>
        public ToolResult Result { get; set; }

        /// <summary>
        /// Whether the tool executed successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Result string from the tool
        /// </summary>
        public string ResultString { get; set; }

        /// <summary>
        /// Error message if the tool failed
        /// </summary>
        public string Error { get; set; }

        public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
        {
            if (string.IsNullOrEmpty(ToolName))
            {
                throw new InvalidOperationException("ToolName is required");
            }

            var tool = _toolRegistry.GetTool(ToolName);
            if (tool == null)
            {
                Result = ToolResult.Failed(ToolCallId, ToolName, $"Tool '{ToolName}' not found");
                Success = false;
                Error = Result.Error;
                ResultString = Result.Result;
                return ExecutionResult.Next();
            }

            try
            {
                var startTime = DateTime.UtcNow;
                Result = await tool.ExecuteAsync(ToolCallId, Arguments, context.CancellationToken);
                Result.Duration = DateTime.UtcNow - startTime;
                
                Success = Result.Success;
                ResultString = Result.Result;
                Error = Result.Error;
            }
            catch (Exception ex)
            {
                Result = ToolResult.Failed(ToolCallId, ToolName, ex.Message);
                Success = false;
                Error = ex.Message;
                ResultString = Result.Result;
            }

            return ExecutionResult.Next();
        }
    }
}
