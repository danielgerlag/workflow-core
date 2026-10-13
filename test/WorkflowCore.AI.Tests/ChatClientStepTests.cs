using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.AI;
using WorkflowCore.Interface;
using Xunit;

namespace WorkflowCore.AI.Tests
{
    public class ChatClientStepTests
    {
        private readonly FakeChatClient _client = new FakeChatClient();
        private readonly IStepExecutionContext _context = A.Fake<IStepExecutionContext>();

        public ChatClientStepTests()
        {
            A.CallTo(() => _context.CancellationToken).Returns(CancellationToken.None);
        }

        [Fact]
        public async Task sends_system_prompt_history_then_prompt_and_returns_reply()
        {
            var step = new ChatClientStep(_client)
            {
                SystemPrompt = "Be brief.",
                Messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "Hi"), new ChatMessage(ChatRole.Assistant, "Hello") },
                Prompt = "What is 2+2?"
            };

            var result = await step.RunAsync(_context);

            result.Proceed.Should().BeTrue();
            _client.LastMessages.Count.Should().Be(4);
            _client.LastMessages[0].Role.Should().Be(ChatRole.System);
            _client.LastMessages[0].Text.Should().Be("Be brief.");
            _client.LastMessages[1].Text.Should().Be("Hi");
            _client.LastMessages[2].Text.Should().Be("Hello");
            _client.LastMessages[3].Role.Should().Be(ChatRole.User);
            _client.LastMessages[3].Text.Should().Be("What is 2+2?");
            step.ResponseText.Should().Be("echo: What is 2+2?");
            step.Response.FinishReason.Should().Be(ChatFinishReason.Stop);
        }

        [Fact]
        public async Task passes_chat_options_through()
        {
            var options = new ChatOptions { ModelId = "test-model", Temperature = 0.1f };
            var step = new ChatClientStep(_client) { Prompt = "x", Options = options };

            await step.RunAsync(_context);

            _client.LastOptions.Should().BeSameAs(options);
        }

        [Fact]
        public async Task works_with_messages_only()
        {
            var step = new ChatClientStep(_client) { Messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, "only history") } };

            await step.RunAsync(_context);

            _client.LastMessages.Count.Should().Be(1);
            step.ResponseText.Should().Be("echo: only history");
        }

        [Fact]
        public async Task throws_without_prompt_or_messages_and_does_not_call_model()
        {
            var step = new ChatClientStep(_client) { SystemPrompt = "Be brief." };

            await Assert.ThrowsAsync<InvalidOperationException>(() => step.RunAsync(_context));
            _client.Calls.Should().Be(0);
        }

        [Fact]
        public void requires_chat_client()
        {
            Assert.Throws<ArgumentNullException>(() => new ChatClientStep(null));
        }
    }
}
