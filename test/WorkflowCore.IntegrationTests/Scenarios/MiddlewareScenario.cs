using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Testing;
using Xunit;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class MiddlewareScenario : WorkflowTest<MiddlewareScenario.MyWorkflow, object>
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(5);
        private readonly List<TestWorkflowMiddleware> _workflowMiddleware = new List<TestWorkflowMiddleware>();
        private readonly List<TestStepMiddleware> _stepMiddleware = new List<TestStepMiddleware>();
        private readonly TestStep _step = new TestStep();

        public MiddlewareScenario()
        {
            Setup();
        }

        public TestWorkflowMiddleware[] PreMiddleware => _workflowMiddleware
            .Where(x => x.Phase == WorkflowMiddlewarePhase.PreWorkflow)
            .ToArray();

        public TestWorkflowMiddleware[] PostMiddleware => _workflowMiddleware
            .Where(x => x.Phase == WorkflowMiddlewarePhase.PostWorkflow)
            .ToArray();

        public class MyWorkflow: IWorkflow<object>
        {
            public string Id => nameof(MyWorkflow);

            public int Version => 1;

            public void Build(IWorkflowBuilder<object> builder) =>
                builder.StartWith<TestStep>();
        }

        public class TestStep : StepBodyAsync
        {

            public DateTime? StartTime { get; private set; }
            public DateTime? EndTime { get; private set; }
            public bool HasCompleted => StartTime.HasValue && EndTime.HasValue;

            public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
            {
                StartTime = DateTime.UtcNow;
                await Task.Delay(Delay);
                EndTime = DateTime.UtcNow;
                return ExecutionResult.Next();
            }
        }

        public class TestWorkflowMiddleware : IWorkflowMiddleware
        {
            public TestWorkflowMiddleware(WorkflowMiddlewarePhase phase)
            {
                Phase = phase;
            }

            public WorkflowMiddlewarePhase Phase { get; }

            public DateTime? StartTime { get; private set; }
            public DateTime? EndTime { get; private set; }
            public bool HasCompleted => StartTime.HasValue && EndTime.HasValue;

            public async Task HandleAsync(WorkflowInstance workflow, WorkflowDelegate next)
            {
                StartTime = DateTime.UtcNow;
                await Task.Delay(Delay);
                await next();
                await Task.Delay(Delay);
                EndTime = DateTime.UtcNow;
            }
        }

        public class TestStepMiddleware : IWorkflowStepMiddleware
        {
            public DateTime? StartTime { get; private set; }
            public DateTime? EndTime { get; private set; }

            public bool HasCompleted => StartTime.HasValue && EndTime.HasValue;

            public async Task<ExecutionResult> HandleAsync(IStepExecutionContext context, IStepBody body, WorkflowStepDelegate next)
            {
                StartTime = DateTime.UtcNow;
                await Task.Delay(Delay);
                var result = await next();
                await Task.Delay(Delay);
                EndTime = DateTime.UtcNow;
                return result;
            }
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.AddTransient(_ => _step);

            // Configure 3 middleware of each type
            const int middlewareCount = 3;
            foreach (var _ in Enumerable.Range(0, middlewareCount))
            {
                var preMiddleware = new TestWorkflowMiddleware(WorkflowMiddlewarePhase.PreWorkflow);
                var postMiddleware = new TestWorkflowMiddleware(WorkflowMiddlewarePhase.PostWorkflow);
                _workflowMiddleware.Add(preMiddleware);
                _workflowMiddleware.Add(postMiddleware);
                services.AddWorkflowMiddleware(p => preMiddleware);
                services.AddWorkflowMiddleware(p => postMiddleware);
            }

            // Configure 3 step middleware
            foreach (var _ in Enumerable.Range(0, middlewareCount))
            {
                var middleware = new TestStepMiddleware();
                services.AddWorkflowStepMiddleware(p => middleware);
                _stepMiddleware.Add(middleware);
            }

        }

        [Fact(DisplayName = "Should run all workflow and step middleware")]
        public async Task Should_run_all_workflow_and_step_middleware()
        {
            var workflowId = await StartWorkflowAsync(new object());
            var status = await WaitForWorkflowToCompleteAsync(workflowId, Timeout);

            // Workflow should complete without errors
            status.Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
            
            // Wait for post middleware to complete
            while (_workflowMiddleware.Any(x => !x.HasCompleted))
            {
                await Task.Delay(500);
            }

            // Each middleware should have run
            _workflowMiddleware.Should()
                .HaveCount(6).And
                .OnlyContain(x => x.HasCompleted);
            _stepMiddleware.Should()
                .HaveCount(3)
                .And
                .OnlyContain(x => x.HasCompleted);

            // Step middleware should have been run in order
            _stepMiddleware.Should().BeInAscendingOrder(x => x.StartTime);
            _stepMiddleware.Should().BeInDescendingOrder(x => x.EndTime);

            // Step should have been called after all step middleware
            _step.HasCompleted.Should().BeTrue();
            _step.StartTime.Should().BeAfter(_stepMiddleware.Last().StartTime.Value);
            _step.EndTime.Should().BeBefore(_stepMiddleware.Last().EndTime.Value);

            // Pre workflow middleware should have been run in order
            PreMiddleware.Should().BeInAscendingOrder(x => x.StartTime);
            PreMiddleware.Should().BeInDescendingOrder(x => x.EndTime);

            // Post workflow middleware should have been run in order
            PostMiddleware.Should().BeInAscendingOrder(x => x.StartTime);
            PostMiddleware.Should().BeInDescendingOrder(x => x.EndTime);
        }
    }
}
