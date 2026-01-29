using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.Inference;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Models;

namespace WorkflowCore.AI.AzureFoundry.Services
{
    /// <summary>
    /// Service for chat completion operations using Azure AI Inference
    /// </summary>
    public class ChatCompletionService : IChatCompletionService
    {
        private readonly AzureFoundryClientFactory _clientFactory;
        private readonly AzureFoundryOptions _options;
        private readonly ILogger<ChatCompletionService> _logger;

        public ChatCompletionService(
            AzureFoundryClientFactory clientFactory,
            IOptions<AzureFoundryOptions> options,
            ILogger<ChatCompletionService> logger)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ChatCompletionResponse> CompleteAsync(
            IEnumerable<ConversationMessage> messages,
            string model = null,
            float? temperature = null,
            int? maxTokens = null,
            IEnumerable<ToolDefinition> tools = null,
            CancellationToken cancellationToken = default)
        {
            var chatMessages = messages.Select(ConvertToSdkMessage).ToList();
            
            var requestOptions = new ChatCompletionsOptions(chatMessages)
            {
                Model = model ?? _options.DefaultModel,
                Temperature = temperature ?? _options.DefaultTemperature,
                MaxTokens = maxTokens ?? _options.DefaultMaxTokens
            };

            if (tools != null && tools.Any())
            {
                foreach (var tool in tools)
                {
                    var functionDef = new FunctionDefinition(tool.Name)
                    {
                        Description = tool.Description,
                        Parameters = BinaryData.FromString(tool.ParametersSchema ?? "{}")
                    };
                    requestOptions.Tools.Add(new ChatCompletionsToolDefinition(functionDef));
                }
            }

            _logger.LogDebug("Sending chat completion request with {MessageCount} messages", chatMessages.Count);

            var client = _clientFactory.CreateChatClient();
            var response = await client.CompleteAsync(requestOptions, cancellationToken);
            var completion = response.Value;

            var responseMessage = new ConversationMessage
            {
                Role = MessageRole.Assistant,
                Content = completion.Content,
                TokenCount = completion.Usage?.TotalTokens
            };

            if (completion.ToolCalls != null && completion.ToolCalls.Any())
            {
                responseMessage.ToolCalls = completion.ToolCalls
                    .Select(tc => new ToolCallRequest
                    {
                        // Use the SDK-provided ID, but ensure it's not too long (API max is 40 chars)
                        Id = EnsureValidToolCallId(tc.Id),
                        ToolName = tc.Function?.Name,
                        Arguments = tc.Function?.Arguments
                    })
                    .ToList();
            }

            return new ChatCompletionResponse
            {
                Message = responseMessage,
                FinishReason = completion.FinishReason?.ToString() ?? "unknown",
                PromptTokens = completion.Usage?.PromptTokens ?? 0,
                CompletionTokens = completion.Usage?.CompletionTokens ?? 0,
                Model = model ?? _options.DefaultModel
            };
        }

        private ChatRequestMessage ConvertToSdkMessage(ConversationMessage message)
        {
            switch (message.Role)
            {
                case MessageRole.System:
                    return new ChatRequestSystemMessage(message.Content);

                case MessageRole.User:
                    return new ChatRequestUserMessage(message.Content);

                case MessageRole.Assistant:
                    var assistantMessage = new ChatRequestAssistantMessage(message.Content ?? string.Empty);
                    if (message.ToolCalls != null)
                    {
                        foreach (var toolCall in message.ToolCalls)
                        {
                            var validId = EnsureValidToolCallId(toolCall.Id);
                            _logger.LogDebug("Assistant tool call ID: original={OriginalLength}, truncated={TruncatedLength}", 
                                toolCall.Id?.Length ?? 0, validId?.Length ?? 0);
                            assistantMessage.ToolCalls.Add(new ChatCompletionsToolCall(
                                validId,
                                new FunctionCall(toolCall.ToolName, toolCall.Arguments)));
                        }
                    }
                    return assistantMessage;

                case MessageRole.Tool:
                    var validToolCallId = EnsureValidToolCallId(message.ToolCallId);
                    _logger.LogDebug("Tool message tool_call_id: original={OriginalLength}, truncated={TruncatedLength}", 
                        message.ToolCallId?.Length ?? 0, validToolCallId?.Length ?? 0);
                    // Constructor order is (content, toolCallId)
                    return new ChatRequestToolMessage(message.Content, validToolCallId);

                default:
                    throw new ArgumentException($"Unknown message role: {message.Role}");
            }
        }

        /// <summary>
        /// Ensures tool call ID is valid (max 40 characters per API requirement)
        /// </summary>
        private static string EnsureValidToolCallId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return "call_" + Guid.NewGuid().ToString("N").Substring(0, 24);
            }
            
            if (id.Length > 40)
            {
                return id.Substring(0, 40);
            }
            
            return id;
        }
    }
}
