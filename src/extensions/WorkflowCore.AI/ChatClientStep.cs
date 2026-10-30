using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.AI
{
    /// <summary>
    /// Sends one chat request to the <see cref="IChatClient"/> registered in DI and exposes the reply.
    /// The request is SystemPrompt (if set), then Messages (if set), then Prompt as a user message (if set).
    /// </summary>
    public class ChatClientStep : StepBodyAsync
    {
        private readonly IChatClient _chatClient;

        public ChatClientStep(IChatClient chatClient)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        }

        public string SystemPrompt { get; set; }

        public IList<ChatMessage> Messages { get; set; }

        public string Prompt { get; set; }

        public ChatOptions Options { get; set; }

        public string ResponseText { get; set; }

        public ChatResponse Response { get; set; }

        public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
        {
            if (string.IsNullOrEmpty(Prompt) && (Messages == null || Messages.Count == 0))
                throw new InvalidOperationException($"{nameof(ChatClientStep)} needs a {nameof(Prompt)} or {nameof(Messages)}.");

            var messages = new List<ChatMessage>();

            if (!string.IsNullOrEmpty(SystemPrompt))
                messages.Add(new ChatMessage(ChatRole.System, SystemPrompt));

            if (Messages != null)
                messages.AddRange(Messages);

            if (!string.IsNullOrEmpty(Prompt))
                messages.Add(new ChatMessage(ChatRole.User, Prompt));

            Response = await _chatClient.GetResponseAsync(messages, Options, context.CancellationToken);
            ResponseText = Response.Text;

            return ExecutionResult.Next();
        }
    }
}
