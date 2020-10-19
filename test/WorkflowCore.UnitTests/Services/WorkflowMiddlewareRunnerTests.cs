using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;
using Xunit;
using Xunit.Abstractions;

namespace WorkflowCore.UnitTests.Services
{
    public class WorkflowMiddlewareRunnerTests
    {
        protected List<IWorkflowMiddleware> Middleware { get; }
        protected WorkflowInstance Workflow { get; }
        protected WorkflowDefinition Definition { get; }
        protected IServiceProvider ServiceProvider { get; }
        protected IWorkflowMiddlewareErrorHandler TopLevelErrorHandler { get; }
        protected IDefLevelErrorHandler DefLevelErrorHandler { get; }
        protected IWorkflowMiddlewareRunner Runner { get; }
        protected ITestOutputHelper Out { get; }

        public WorkflowMiddlewareRunnerTests(ITestOutputHelper output)
        {
            Out = output;
            Middleware = new List<IWorkflowMiddleware>();
            Workflow = new WorkflowInstance();
            Definition = new WorkflowDefinition();
            TopLevelErrorHandler = A.Fake<IWorkflowMiddlewareErrorHandler>();
            DefLevelErrorHandler = A.Fake<IDefLevelErrorHandler>();
            ServiceProvider = new ServiceCollection()
                .AddTransient(_ => TopLevelErrorHandler)
                .AddTransient(_ => DefLevelErrorHandler)
                .BuildServiceProvider();

            A
                .CallTo(HandleMethodFor(TopLevelErrorHandler))
                .Returns(Task.CompletedTask);
            A
                .CallTo(HandleMethodFor(DefLevelErrorHandler))
                .Returns(Task.CompletedTask);

            Runner = new WorkflowMiddlewareRunner(Middleware, ServiceProvider);
        }


        [Fact(DisplayName = "RunPreMiddleware should run nothing when no middleware")]
        public void RunPreMiddleware_should_run_nothing_when_no_middleware()
        {
            // Act
            Func<Task> action = async () => await Runner.RunPreMiddleware(Workflow, Definition);

            // Assert
            action.ShouldNotThrow();
        }

        [Fact(DisplayName = "RunPreMiddleware should run middleware when one middleware")]
        public async Task RunPreMiddleware_should_run_middleware_when_one_middleware()
        {
            // Arrange
            var middleware = BuildWorkflowMiddleware(WorkflowMiddlewarePhase.PreWorkflow);
            Middleware.Add(middleware);

            // Act
            await Runner.RunPreMiddleware(Workflow, Definition);

            // Assert
            A
                .CallTo(HandleMethodFor(middleware))
                .MustHaveHappenedOnceExactly();
        }

        [Fact(DisplayName = "RunPreMiddleware should run all middleware when multiple middleware")]
        public async Task RunPreMiddleware_should_run_all_middleware_when_multiple_middleware()
        {
            // Arrange
            var middleware1 = BuildWorkflowMiddleware(WorkflowMiddlewarePhase.PreWorkflow, 1);
            var middleware2 = BuildWorkflowMiddleware(WorkflowMiddlewarePhase.PreWorkflow, 2);
            var middleware3 = BuildWorkflowMiddleware(WorkflowMiddlewarePhase.PreWorkflow, 3);
            Middleware.AddRange(new[] { middleware1, middleware2, middleware3 });

            // Act
            await Runner.RunPreMiddleware(Workflow, Definition);

            // Assert
            A
                .CallTo(HandleMethodFor(middleware3))
                .MustHaveHappenedOnceExactly()
                .Then(A
                    .CallTo(HandleMethodFor(middleware2))
                    .MustHaveHappenedOnceExactly())
                .Then(A
                    .CallTo(HandleMethodFor(middleware1))
                    .MustHaveHappenedOnceExactly());
        }

        [Fact(DisplayName = "RunPreMiddleware should only run middleware in PreWorkflow phase")]
        public async Task RunPreMiddleware_should_only_run_middleware_in_PreWorkflow_phase()
        {
            // Arrange
            var preMiddleware1 = BuildWorkflowMiddleware(WorkflowMiddlewarePhase.PreWorkflow, 1);
            var preMiddleware2 = BuildWorkflowMiddleware(WorkflowMiddlewarePhase.PreWorkflow, 2);
            var postMiddleware1 = BuildWorkflowMiddleware(WorkflowMiddlewarePhase.PostWorkflow, 3);
            var postMiddleware2 = BuildWorkflowMiddleware(WorkflowMiddlewarePhase.PostWorkflow, 4);
            Middleware.AddRange(new[] { postMiddleware1, postMiddleware2, preMiddleware1, preMiddleware2 });

            // Act
            await Runner.RunPreMiddleware(Workflow, Definition);

            // Assert
            A
                .CallTo(HandleMethodFor(preMiddleware2))
                .MustHaveHappenedOnceExactly()
                .Then(A
                    .CallTo(HandleMethodFor(preMiddleware1))
                    .MustHaveHappenedOnceExactly());

            A.CallTo(HandleMethodFor(postMiddleware1)).MustNotHaveHappened();
            A.CallTo(HandleMethodFor(postMiddleware2)).MustNotHaveHappened();
        }

        [Fact(DisplayName = "RunPostMiddleware should run nothing when no middleware")]
        public void RunPostMiddleware_should_run_nothing_when_no_middleware()
        {
            // Act
            Func<Task> action = async () => await Runner.RunPostMiddleware(Workflow, Definition);

            // Assert
            action.ShouldNotThrow();
        }

        [Fact(DisplayName = "RunPostMiddleware should run middleware when one middleware")]
        public async Task RunPostMiddleware_should_run_middleware_when_one_middleware()
        {
            // Arrange
            var middleware = BuildWorkflowMiddleware(WorkflowMiddlewarePhase.PostWorkflow);
            Middleware.Add(middleware);

            // Act
            await Runner.RunPostMiddleware(Workflow, Definition);

            // Assert
            A
                .CallTo(HandleMethodFor(middleware))
                .MustHaveHappenedOnceExactly();
        }

        [Fact(DisplayName = "RunPostMiddleware should run all middleware when multiple middleware")]
        public async Task RunPostMiddleware_should_run_all_middleware_when_multiple_middleware()
        {
            // Arrange
            var middleware1 = BuildWorkflowMiddleware(WorkflowMiddlewarePhase.PostWorkflow, 1);
            var middleware2 = BuildWorkflowMiddleware(WorkflowMiddlewarePhase.PostWorkflow, 2);
            var middleware3 = BuildWorkflowMiddleware(WorkflowMiddlewarePhase.PostWorkflow, 3);
            Middleware.AddRange(new[] { middleware1, middleware2, middleware3 });

            // Act
            await Runner.RunPostMiddleware(Workflow, Definition);

            // Assert
            A
                .CallTo(HandleMethodFor(middleware3))
                .MustHaveHappenedOnceExactly()
                .Then(A
                    .CallTo(HandleMethodFor(middleware2))
                    .MustHaveHappenedOnceExactly())
                .Then(A
                    .CallTo(HandleMethodFor(middleware1))
                    .MustHaveHappenedOnceExactly());
        }

        [Fact(DisplayName = "RunPostMiddleware should only run middleware in PostWorkflow phase")]
        public async Task RunPostMiddleware_should_only_run_middleware_in_PostWorkflow_phase()
        {
            // Arrange
            var postMiddleware1 = BuildWorkflowMiddleware(WorkflowMiddlewarePhase.PostWorkflow, 1);
            var postMiddleware2 = BuildWorkflowMiddleware(WorkflowMiddlewarePhase.PostWorkflow, 2);
            var preMiddleware1 = BuildWorkflowMiddleware(WorkflowMiddlewarePhase.PreWorkflow, 3);
            var preMiddleware2 = BuildWorkflowMiddleware(WorkflowMiddlewarePhase.PreWorkflow, 4);
            Middleware.AddRange(new[] { preMiddleware1, postMiddleware1, preMiddleware2, postMiddleware2 });

            // Act
            await Runner.RunPostMiddleware(Workflow, Definition);

            // Assert
            A
                .CallTo(HandleMethodFor(postMiddleware2))
                .MustHaveHappenedOnceExactly()
                .Then(A
                    .CallTo(HandleMethodFor(postMiddleware1))
                    .MustHaveHappenedOnceExactly());

            A.CallTo(HandleMethodFor(preMiddleware1)).MustNotHaveHappened();
            A.CallTo(HandleMethodFor(preMiddleware1)).MustNotHaveHappened();
        }

        [Fact(DisplayName = "RunPostMiddleware should call top level error handler when middleware throws")]
        public async Task RunPostMiddleware_should_call_top_level_error_handler_when_middleware_throws()
        {
            // Arrange
            var middleware = BuildWorkflowMiddleware(WorkflowMiddlewarePhase.PostWorkflow, 1);
            A.CallTo(HandleMethodFor(middleware)).ThrowsAsync(new ApplicationException("Something went wrong"));
            Middleware.AddRange(new[] { middleware });

            // Act
            await Runner.RunPostMiddleware(Workflow, Definition);

            // Assert
            A
                .CallTo(HandleMethodFor(TopLevelErrorHandler))
                .MustHaveHappenedOnceExactly();
        }

        [Fact(DisplayName =
            "RunPostMiddleware should call error handler on workflow def when middleware throws and def has handler defined")]
        public async Task
            RunPostMiddleware_should_call_error_handler_on_workflow_def_when_middleware_throws_and_def_has_handler()
        {
            // Arrange
            var middleware = BuildWorkflowMiddleware(WorkflowMiddlewarePhase.PostWorkflow, 1);
            A.CallTo(HandleMethodFor(middleware)).ThrowsAsync(new ApplicationException("Something went wrong"));
            Middleware.AddRange(new[] { middleware });
            Definition.OnPostMiddlewareError = typeof(IDefLevelErrorHandler);

            // Act
            await Runner.RunPostMiddleware(Workflow, Definition);

            // Assert
            A
                .CallTo(HandleMethodFor(TopLevelErrorHandler))
                .MustNotHaveHappened();
            A
                .CallTo(HandleMethodFor(DefLevelErrorHandler))
                .MustHaveHappenedOnceExactly();
        }

        #region Helpers

        private IWorkflowMiddleware BuildWorkflowMiddleware(
            WorkflowMiddlewarePhase phase,
            int id = 0
        )
        {
            var middleware = A.Fake<IWorkflowMiddleware>();
            A.CallTo(() => middleware.Phase).Returns(phase);
            A
                .CallTo(HandleMethodFor(middleware))
                .ReturnsLazily(async call =>
                {
                    Out.WriteLine($@"Before workflow middleware {id}");
                    await call.Arguments[1].As<WorkflowDelegate>().Invoke();
                    Out.WriteLine($@"After workflow middleware {id}");
                });

            return middleware;
        }

        private static Expression<Func<Task>> HandleMethodFor(IWorkflowMiddleware middleware) =>
            () => middleware.HandleAsync(
                A<WorkflowInstance>._,
                A<WorkflowDelegate>._);

        private static Expression<Func<Task>> HandleMethodFor(IWorkflowMiddlewareErrorHandler errorHandler) =>
            () => errorHandler.HandleAsync(A<Exception>._);

        public interface IDefLevelErrorHandler : IWorkflowMiddlewareErrorHandler
        {
        }

        #endregion
    }
}
