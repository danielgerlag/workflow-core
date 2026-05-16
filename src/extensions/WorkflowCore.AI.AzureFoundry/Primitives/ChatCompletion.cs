using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Models;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.AI.AzureFoundry.Primitives
{
    /// <summary>
    /// Step body for chat completion operations
    /// </summary>
    public class ChatCompletion : StepBodyAsync
    {
        private readonly IChatCompletionService _chatService;
        private readonly IConversationStore _conversationStore;

        public ChatCompletion(IChatCompletionService chatService, IConversationStore conversationStore)
        {
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _conversationStore = conversationStore ?? throw new ArgumentNullException(nameof(conversationStore));
        }

        /// <summary>
        /// System prompt to set the LLM's behavior
        /// </summary>
        public string SystemPrompt { get; set; }

        /// <summary>
        /// User message to send to the LLM
        /// </summary>
        public string UserMessage { get; set; }

        /// <summary>
        /// Model to use (optional, uses default if not specified)
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Temperature for response generation (0.0 - 2.0)
        /// </summary>
        public float? Temperature { get; set; }

        /// <summary>
        /// Maximum tokens in the response
        /// </summary>
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Whether to include conversation history from previous steps
        /// </summary>
        public bool IncludeHistory { get; set; } = true;

        /// <summary>
        /// Thread ID for conversation history (optional)
        /// </summary>
        public string ThreadId { get; set; }

        // Outputs

        /// <summary>
        /// The generated response text
        /// </summary>
        public string Response { get; set; }

        /// <summary>
        /// Reason the completion finished
        /// </summary>
        public string FinishReason { get; set; }

        /// <summary>
        /// Number of tokens used in the prompt
        /// </summary>
        public int PromptTokens { get; set; }

        /// <summary>
        /// Number of tokens used in the completion
        /// </summary>
        public int CompletionTokens { get; set; }

        /// <summary>
        /// Total tokens used
        /// </summary>
        public int TotalTokens { get; set; }

        public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
        {
            var messages = new List<ConversationMessage>();

            if (IncludeHistory && !string.IsNullOrEmpty(ThreadId))
            {
                var thread = await _conversationStore.GetThreadAsync(ThreadId);
                if (thread != null)
                {
                    messages.AddRange(thread.Messages);
                }
            }
            else if (IncludeHistory)
            {
                var thread = await _conversationStore.GetOrCreateThreadAsync(
                    context.Workflow.Id,
                    context.ExecutionPointer.Id);
                messages.AddRange(thread.Messages);
                ThreadId = thread.Id;
            }

            if (!string.IsNullOrEmpty(SystemPrompt) && (messages.Count == 0 || messages[0].Role != MessageRole.System))
            {
                messages.Insert(0, new ConversationMessage
                {
                    Role = MessageRole.System,
                    Content = SystemPrompt
                });
            }

            messages.Add(new ConversationMessage
            {
                Role = MessageRole.User,
                Content = UserMessage
            });

            var result = await _chatService.CompleteAsync(
                messages,
                Model,
                Temperature,
                MaxTokens,
                cancellationToken: context.CancellationToken);

            Response = result.Message.Content;
            FinishReason = result.FinishReason;
            PromptTokens = result.PromptTokens;
            CompletionTokens = result.CompletionTokens;
            TotalTokens = result.PromptTokens + result.CompletionTokens;

            if (IncludeHistory)
            {
                var thread = await _conversationStore.GetThreadAsync(ThreadId) 
                    ?? await _conversationStore.GetOrCreateThreadAsync(context.Workflow.Id, context.ExecutionPointer.Id);

                thread.AddUserMessage(UserMessage);
                thread.AddAssistantMessage(Response);
                await _conversationStore.SaveThreadAsync(thread);
            }

            return ExecutionResult.Next();
        }
    }
}
