using System;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Testing;
using Xunit;

namespace WorkflowCore.AI.Tests
{
    public class SummaryData
    {
        public string Ticket { get; set; }
        public string Summary { get; set; }
    }

    public class SummarizeTicketWorkflow : IWorkflow<SummaryData>
    {
        public string Id => "summarize-ticket";
        public int Version => 1;

        public void Build(IWorkflowBuilder<SummaryData> builder)
        {
            builder
                .StartWith<ChatClientStep>()
                    .Input(step => step.SystemPrompt, data => "Summarize in one sentence.")
                    .Input(step => step.Prompt, data => data.Ticket)
                    .Output(data => data.Summary, step => step.ResponseText);
        }
    }

    public class ChatClientWorkflowScenario : WorkflowTest<SummarizeTicketWorkflow, SummaryData>
    {
        private readonly FakeChatClient _client = new FakeChatClient();

        public ChatClientWorkflowScenario()
        {
            Setup();
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddSingleton<IChatClient>(_client);
            services.AddWorkflowChatClientStep();
        }

        [Fact]
        public void workflow_resolves_chat_client_from_di_and_maps_reply_to_data()
        {
            var workflowId = StartWorkflow(new SummaryData { Ticket = "Printer on floor 3 is jammed" });
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(10));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Should().BeEmpty();
            GetData(workflowId).Summary.Should().Be("echo: Printer on floor 3 is jammed");
            _client.Calls.Should().Be(1);
        }
    }
}
