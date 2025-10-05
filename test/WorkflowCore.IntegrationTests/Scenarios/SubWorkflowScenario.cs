using System;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Testing;
using Xunit;
using Xunit.Abstractions;

namespace WorkflowCore.IntegrationTests.Scenarios;

public class SubWorkflowScenario : WorkflowTest<SubWorkflowScenario.ParentWorkflow, SubWorkflowScenario.ApprovalInput>
{
    public class ApprovalInput
    {
        public string Id { get; set; }
        public bool Approved { get; set; }
        public TimeSpan TimeSpan { get; set; }
        public string Message { get; set; }
    }

    public class ParentWorkflow : IWorkflow<ApprovalInput>
    {
        public string Id => nameof(ParentWorkflow);

        public int Version => 1;
        
        public void Build(IWorkflowBuilder<ApprovalInput> builder)
        {
            builder
                .UseDefaultErrorBehavior(WorkflowErrorHandling.Terminate)
                .StartWith(context => ExecutionResult.Next())
                .SubWorkflow(nameof(ChildWorkflow))
                .Output(i => i.Approved, step => ((ApprovalInput)step.Result).Approved)
                /*
                 * this does throw an exception
                 .If(data => data.Approved)
                    .Do(then =>
                        ExecutionResult.Outcome(1248))*/;
        }
    }
    
    public class ChildWorkflow : IWorkflow<ApprovalInput>
    {
        public string Id => nameof(ChildWorkflow);

        public int Version => 1;

        public void Build(IWorkflowBuilder<ApprovalInput> builder)
        {
            builder
                .UseDefaultErrorBehavior(WorkflowErrorHandling.Terminate)
                .StartWith(context => ExecutionResult.Next())
                .Parallel()
                .Do(then
                    => then
                        .Delay(i => i.TimeSpan)
                        .Output(i => i.Approved, step => false)
                        .EndWorkflow()
                )
                .Do(then
                    => then
                        .WaitFor("Approved", e => e.Id)
                        .Output((w, input) =>
                        {
                            var j = JObject.FromObject(w.EventData);
                            input.Approved = j["Approved"].Value<bool>();
                            input.Message= j["Message"].Value<string>();
                        })
                        .EndWorkflow()
                )
                .Join();
        }
    }

    public SubWorkflowScenario()
    {
        Setup();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Scenario(bool approved)
    {
        Host.Registry.RegisterWorkflow(new ChildWorkflow());
        
        var eventKey = Guid.NewGuid().ToString();
        var workflowId = StartWorkflow(new ApprovalInput
        {
            Id = eventKey, 
            TimeSpan = TimeSpan.FromMinutes(10)
        });
        
        WaitForEventSubscription("Approved", workflowId, TimeSpan.FromSeconds(5));
        UnhandledStepErrors.Should().BeEmpty();

        Host.PublishEvent("Approved", workflowId, new
        {
            Approved = approved, 
            Message = "message " + approved 
        });

        WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(10));
        
        System.Threading.Thread.Sleep(2000);

        UnhandledStepErrors.Should().BeEmpty();
        GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
        GetData(workflowId).ShouldBeEquivalentTo(new ApprovalInput
        {
            Id = eventKey,
            Approved = approved,
            Message = "message " + approved,
            TimeSpan = TimeSpan.FromMinutes(10)
        });
    }
    
    [Fact]
    public void Failure()
    {
        Host.Registry.RegisterWorkflow(new ChildWorkflow());
        
        var eventKey = Guid.NewGuid().ToString();
        var workflowId = StartWorkflow(new ApprovalInput
        {
            Id = eventKey, 
            TimeSpan = TimeSpan.FromMinutes(10)
        });
        
        WaitForEventSubscription("Approved", workflowId, TimeSpan.FromSeconds(5));
        UnhandledStepErrors.Should().BeEmpty();

        Host.PublishEvent("Approved", workflowId, new
        {
            Approved = "string" 
        });

        WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(20));
        
        System.Threading.Thread.Sleep(2000);

        UnhandledStepErrors.Should().NotBeEmpty();
        GetStatus(workflowId).Should().Be(WorkflowStatus.Terminated);
    }
    
    [Fact]
    public void Timeout()
    {
        Host.Registry.RegisterWorkflow(new ChildWorkflow());
        
        var workflowId = StartWorkflow(new ApprovalInput
        {
            Id = Guid.NewGuid().ToString(),
            TimeSpan = TimeSpan.FromSeconds(5)
        });
        WaitForEventSubscription("Approved", workflowId, TimeSpan.FromSeconds(2));
        WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(10));

        UnhandledStepErrors.Should().BeEmpty();
        GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
        GetData(workflowId).Approved.Should().BeFalse();
    }
}
