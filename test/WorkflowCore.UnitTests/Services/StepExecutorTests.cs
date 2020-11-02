using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;
using Xunit;
using Xunit.Abstractions;

namespace WorkflowCore.UnitTests.Services
{
    public class StepExecutorTests
    {
        protected List<IWorkflowStepMiddleware> Middleware { get; }
        protected IStepBody Body { get; }
        protected IStepExecutionContext Context { get; }
        protected IStepExecutor Runner { get; }
        protected ExecutionResult DummyResult { get; } = ExecutionResult.Persist(null);
        protected ITestOutputHelper Out { get; }

        public StepExecutorTests(ITestOutputHelper output)
        {
            Middleware = new List<IWorkflowStepMiddleware>();
            Body = A.Fake<IStepBody>();
            Context = A.Fake<IStepExecutionContext>();
            Out = output;
            Runner = new StepExecutor(Middleware);

            A
                .CallTo(() => Body.RunAsync(A<IStepExecutionContext>._))
                .Invokes(() => Out.WriteLine("Called step body"))
                .Returns(DummyResult);
        }

        [Fact(DisplayName = "ExecuteStep should run step when no middleware")]
        public async Task ExecuteStep_should_run_step_when_no_middleware()
        {
            // Act
            var result = await Runner.ExecuteStep(Context, Body);

            // Assert
            result.Should().Be(DummyResult);
        }

        [Fact(DisplayName = "ExecuteStep should run middleware and step when one middleware")]
        public async Task ExecuteStep_should_run_middleware_and_step_when_one_middleware()
        {
            // Arrange
            var middleware = BuildStepMiddleware();
            Middleware.Add(middleware);

            // Act
            var result = await Runner.ExecuteStep(Context, Body);

            // Assert
            result.Should().Be(DummyResult);
            A
                .CallTo(RunMethodFor(Body))
                .MustHaveHappenedOnceExactly()
                .Then(
                    A.CallTo(HandleMethodFor(middleware))
                        .MustHaveHappenedOnceExactly());
        }

        [Fact(DisplayName =
            "ExecuteStep should run middleware chain completing in reverse order and step when multiple middleware")]
        public async Task
            ExecuteStep_should_run_middleware_chain_completing_in_reverse_order_and_step_when_multiple_middleware()
        {
            // Arrange
            var middleware1 = BuildStepMiddleware(1);
            var middleware2 = BuildStepMiddleware(2);
            var middleware3 = BuildStepMiddleware(3);
            Middleware.AddRange(new[] { middleware1, middleware2, middleware3 });

            // Act
            var result = await Runner.ExecuteStep(Context, Body);

            // Assert
            result.Should().Be(DummyResult);
            A
                .CallTo(RunMethodFor(Body))
                .MustHaveHappenedOnceExactly()
                .Then(A
                    .CallTo(HandleMethodFor(middleware3))
                    .MustHaveHappenedOnceExactly())
                .Then(A
                    .CallTo(HandleMethodFor(middleware2))
                    .MustHaveHappenedOnceExactly())
                .Then(A
                    .CallTo(HandleMethodFor(middleware1))
                    .MustHaveHappenedOnceExactly());
        }

        [Fact(DisplayName = "ExecuteStep should bubble up exceptions in middleware")]
        public void ExecuteStep_should_bubble_up_exceptions_in_middleware()
        {
            // Arrange
            var middleware1 = BuildStepMiddleware(1);
            var middleware2 = BuildStepMiddleware(2);
            var middleware3 = BuildStepMiddleware(3);
            Middleware.AddRange(new[] { middleware1, middleware2, middleware3 });
            A
                .CallTo(HandleMethodFor(middleware2))
                .Throws(new ApplicationException("Failed"));

            // Act
            Func<Task<ExecutionResult>> action = async () => await Runner.ExecuteStep(Context, Body);

            // Assert
            action
                .ShouldThrow<ApplicationException>()
                .WithMessage("Failed");
        }

        #region Helpers

        private IWorkflowStepMiddleware BuildStepMiddleware(int id = 0)
        {
            var middleware = A.Fake<IWorkflowStepMiddleware>();
            A
                .CallTo(HandleMethodFor(middleware))
                .ReturnsLazily(async call =>
                {
                    Out.WriteLine($@"Before step middleware {id}");
                    var result = await call.Arguments[2].As<WorkflowStepDelegate>().Invoke();
                    Out.WriteLine($@"After step middleware {id}");
                    return result;
                });

            return middleware;
        }

        private static Expression<Func<Task<ExecutionResult>>> HandleMethodFor(IWorkflowStepMiddleware middleware) =>
            () => middleware.HandleAsync(
                A<IStepExecutionContext>._,
                A<IStepBody>._,
                A<WorkflowStepDelegate>._);

        private static Expression<Func<Task<ExecutionResult>>> RunMethodFor(IStepBody body) =>
            () => body.RunAsync(A<IStepExecutionContext>._);

        #endregion
    }
}
