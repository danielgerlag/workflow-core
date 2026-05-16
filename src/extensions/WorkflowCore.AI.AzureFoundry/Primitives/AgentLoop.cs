using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Models;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.AI.AzureFoundry.Primitives
{
    /// <summary>
    /// Step body for running an agent loop (LLM with automatic tool execution)
    /// </summary>
    public class AgentLoop : StepBodyAsync
    {
        private readonly IChatCompletionService _chatService;
        private readonly IToolRegistry _toolRegistry;
        private readonly IConversationStore _conversationStore;

        public AgentLoop(
            IChatCompletionService chatService,
            IToolRegistry toolRegistry,
            IConversationStore conversationStore)
        {
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
            _conversationStore = conversationStore ?? throw new ArgumentNullException(nameof(conversationStore));
        }

        /// <summary>
        /// System prompt to set the agent's behavior
        /// </summary>
        public string SystemPrompt { get; set; }

        /// <summary>
        /// User message to start the agent loop
        /// </summary>
        public string UserMessage { get; set; }

        /// <summary>
        /// Model to use (optional, uses default if not specified)
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Temperature for response generation
        /// </summary>
        public float? Temperature { get; set; }

        /// <summary>
        /// Maximum number of iterations (LLM calls) before stopping
        /// </summary>
        public int MaxIterations { get; set; } = 10;

        /// <summary>
        /// Whether to run in automatic mode (execute tools automatically)
        /// </summary>
        public bool AutomaticMode { get; set; } = true;

        /// <summary>
        /// Names of tools available to the agent (uses all registered tools if empty)
        /// </summary>
        public IList<string> AvailableTools { get; set; } = new List<string>();

        /// <summary>
        /// Thread ID for conversation history (optional)
        /// </summary>
        public string ThreadId { get; set; }

        // Outputs

        /// <summary>
        /// Final response from the agent
        /// </summary>
        public string Response { get; set; }

        /// <summary>
        /// Number of iterations executed
        /// </summary>
        public int IterationsExecuted { get; set; }

        /// <summary>
        /// Tool calls that were made during the loop
        /// </summary>
        public IList<ToolResult> ToolResults { get; set; } = new List<ToolResult>();

        /// <summary>
        /// Total tokens used across all iterations
        /// </summary>
        public int TotalTokens { get; set; }

        /// <summary>
        /// Whether the loop completed successfully (vs hitting max iterations)
        /// </summary>
        public bool CompletedSuccessfully { get; set; }

        public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
        {
            var thread = await GetOrCreateThread(context);
            
            if (!string.IsNullOrEmpty(SystemPrompt) && 
                (thread.Messages.Count == 0 || thread.Messages[0].Role != MessageRole.System))
            {
                thread.AddSystemMessage(SystemPrompt);
            }

            thread.AddUserMessage(UserMessage);

            var tools = GetAvailableTools();
            var toolDefinitions = tools.Select(t => new ToolDefinition
            {
                Name = t.Name,
                Description = t.Description,
                ParametersSchema = t.ParametersSchema
            }).ToList();

            for (int iteration = 0; iteration < MaxIterations; iteration++)
            {
                IterationsExecuted = iteration + 1;

                var result = await _chatService.CompleteAsync(
                    thread.Messages,
                    Model,
                    Temperature,
                    cancellationToken: context.CancellationToken,
                    tools: toolDefinitions);

                TotalTokens += result.TotalTokens;
                thread.AddMessage(result.Message);

                if (result.FinishReason == "stop" || result.Message.ToolCalls == null || !result.Message.ToolCalls.Any())
                {
                    Response = result.Message.Content;
                    CompletedSuccessfully = true;
                    await _conversationStore.SaveThreadAsync(thread);
                    return ExecutionResult.Next();
                }

                if (!AutomaticMode)
                {
                    Response = result.Message.Content;
                    await _conversationStore.SaveThreadAsync(thread);
                    return ExecutionResult.Next();
                }

                foreach (var toolCall in result.Message.ToolCalls)
                {
                    var tool = tools.FirstOrDefault(t => t.Name == toolCall.ToolName);
                    ToolResult toolResult;

                    if (tool == null)
                    {
                        toolResult = ToolResult.Failed(toolCall.Id, toolCall.ToolName, $"Tool '{toolCall.ToolName}' not found");
                    }
                    else
                    {
                        try
                        {
                            toolResult = await tool.ExecuteAsync(toolCall.Id, toolCall.Arguments, context.CancellationToken);
                        }
                        catch (Exception ex)
                        {
                            toolResult = ToolResult.Failed(toolCall.Id, toolCall.ToolName, ex.Message);
                        }
                    }

                    ToolResults.Add(toolResult);
                    thread.AddToolMessage(toolCall.Id, toolCall.ToolName, toolResult.Result);
                }
            }

            CompletedSuccessfully = false;
            Response = thread.Messages.LastOrDefault(m => m.Role == MessageRole.Assistant)?.Content;
            await _conversationStore.SaveThreadAsync(thread);
            
            return ExecutionResult.Next();
        }

        private async Task<ConversationThread> GetOrCreateThread(IStepExecutionContext context)
        {
            if (!string.IsNullOrEmpty(ThreadId))
            {
                var existing = await _conversationStore.GetThreadAsync(ThreadId);
                if (existing != null)
                    return existing;
            }

            var thread = await _conversationStore.GetOrCreateThreadAsync(
                context.Workflow.Id,
                context.ExecutionPointer.Id);
            ThreadId = thread.Id;
            return thread;
        }

        private IList<IAgentTool> GetAvailableTools()
        {
            if (AvailableTools != null && AvailableTools.Any())
            {
                return AvailableTools
                    .Select(name => _toolRegistry.GetTool(name))
                    .Where(t => t != null)
                    .ToList();
            }

            return _toolRegistry.GetAllTools().ToList();
        }
    }
}
