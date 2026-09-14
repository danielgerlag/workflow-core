using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;
using Xunit;
using System.Threading;

namespace WorkflowCore.UnitTests.Services
{
    public class SyncWorkflowRunnerTests
    {
        protected ISyncWorkflowRunner Subject;
        protected IWorkflowHost Host;
        protected IPersistenceProvider PersistenceProvider;
        protected IWorkflowRegistry Registry;
        protected IExecutionResultProcessor ResultProcesser;
        protected ILifeCycleEventPublisher EventHub;
        protected ICancellationProcessor CancellationProcessor;
        protected IServiceProvider ServiceProvider;
        protected IScopeProvider ScopeProvider;
        protected IDateTimeProvider DateTimeProvider;
        protected IStepExecutor StepExecutor;
        protected IExecutionPointerFactory PointerFactory;
        protected IDistributedLockProvider LockService;
        protected IWorkflowMiddlewareRunner MiddlewareRunner;
        protected WorkflowOptions Options;
        protected WorkflowExecutor Executor;

        public SyncWorkflowRunnerTests()
        {
            Host = A.Fake<IWorkflowHost>();
            PersistenceProvider = A.Fake<IPersistenceProvider>();
            ServiceProvider = A.Fake<IServiceProvider>();
            ScopeProvider = A.Fake<IScopeProvider>();
            Registry = A.Fake<IWorkflowRegistry>();
            ResultProcesser = A.Fake<IExecutionResultProcessor>();
            EventHub = A.Fake<ILifeCycleEventPublisher>();
            CancellationProcessor = A.Fake<ICancellationProcessor>();
            DateTimeProvider = A.Fake<IDateTimeProvider>();
            MiddlewareRunner = A.Fake<IWorkflowMiddlewareRunner>();
            StepExecutor = A.Fake<IStepExecutor>();
            PointerFactory = new ExecutionPointerFactory();
            LockService = A.Fake<IDistributedLockProvider>();

            Options = new WorkflowOptions(A.Fake<IServiceCollection>());

            var stepExecutionScope = A.Fake<IServiceScope>();
            A.CallTo(() => ScopeProvider.CreateScope(A<IStepExecutionContext>._)).Returns(stepExecutionScope);
            A.CallTo(() => stepExecutionScope.ServiceProvider).Returns(ServiceProvider);

            var scope = A.Fake<IServiceScope>();
            var scopeFactory = A.Fake<IServiceScopeFactory>();
            A.CallTo(() => ServiceProvider.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactory);
            A.CallTo(() => scopeFactory.CreateScope()).Returns(scope);
            A.CallTo(() => scope.ServiceProvider).Returns(ServiceProvider);

            A.CallTo(() => DateTimeProvider.Now).Returns(DateTime.Now);
            A.CallTo(() => DateTimeProvider.UtcNow).Returns(DateTime.UtcNow);

            A
                .CallTo(() => ServiceProvider.GetService(typeof(IWorkflowMiddlewareRunner)))
                .Returns(MiddlewareRunner);

            A
                .CallTo(() => ServiceProvider.GetService(typeof(IStepExecutor)))
                .Returns(StepExecutor);

            A.CallTo(() => MiddlewareRunner
                    .RunPostMiddleware(A<WorkflowInstance>._, A<WorkflowDefinition>._))
                .Returns(Task.CompletedTask);

            A.CallTo(() => MiddlewareRunner
                    .RunExecuteMiddleware(A<WorkflowInstance>._, A<WorkflowDefinition>._))
                .Returns(Task.CompletedTask);

            A.CallTo(() => StepExecutor.ExecuteStep(A<IStepExecutionContext>._, A<IStepBody>._))
                .ReturnsLazily(call =>
                    call.Arguments[1].As<IStepBody>().RunAsync(
                        call.Arguments[0].As<IStepExecutionContext>()));

            A.CallTo(() => PersistenceProvider.CreateNewWorkflow(A<WorkflowInstance>.Ignored, A<CancellationToken>.Ignored)).Returns(Guid.NewGuid().ToString());

            A.CallTo(() => LockService.AcquireLock(A<string>._, A<CancellationToken>._)).Returns(true);

            //config logging
            var loggerFactory = new LoggerFactory();
            //loggerFactory.AddConsole(LogLevel.Debug);

            Executor = new WorkflowExecutor(Registry, ServiceProvider, ScopeProvider, DateTimeProvider, ResultProcesser, EventHub, CancellationProcessor, Options, loggerFactory);
            Subject = new SyncWorkflowRunner(Host, Executor, LockService, Registry, PersistenceProvider, PointerFactory, A.Fake<IQueueProvider>(), DateTimeProvider, MiddlewareRunner);
        }

        [Fact(DisplayName = "Should run pre-middlewares for sync workflows")]
        public async Task should_run_pre_middlewares()
        {
            //arrange
            var step1Body = A.Fake<IStepBody>();
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Returns(ExecutionResult.Next());
            WorkflowStep step1 = BuildFakeStep(step1Body);
            Given1StepWorkflow(step1, "Workflow", 1);

            //act
            await Subject.RunWorkflowSync("Workflow", 1, new { }, "Test", TimeSpan.FromMilliseconds(1));

            //assert
            A.CallTo(() => MiddlewareRunner.RunPreMiddleware(A<WorkflowInstance>.Ignored, A<WorkflowDefinition>.Ignored)).MustHaveHappened();
        }

        private void Given1StepWorkflow(WorkflowStep step1, string id, int version)
        {
            A.CallTo(() => Registry.GetDefinition(id, version)).Returns(new WorkflowDefinition
            {
                Id = id,
                Version = version,
                DataType = typeof(object),
                Steps = new WorkflowStepCollection
                {
                    step1
                }
            });
        }

        private WorkflowStep BuildFakeStep(IStepBody stepBody)
        {
            return BuildFakeStep(stepBody, new List<IStepParameter>(), new List<IStepParameter>());
        }

        private WorkflowStep BuildFakeStep(IStepBody stepBody, List<IStepParameter> inputs, List<IStepParameter> outputs)
        {
            var result = A.Fake<WorkflowStep>();
            A.CallTo(() => result.Id).Returns(0);
            A.CallTo(() => result.BodyType).Returns(stepBody.GetType());
            A.CallTo(() => result.ResumeChildrenAfterCompensation).Returns(true);
            A.CallTo(() => result.RevertChildrenAfterCompensation).Returns(false);
            A.CallTo(() => result.ConstructBody(ServiceProvider)).Returns(stepBody);
            A.CallTo(() => result.Inputs).Returns(inputs);
            A.CallTo(() => result.Outputs).Returns(outputs);
            A.CallTo(() => result.Outcomes).Returns(new List<IStepOutcome>());
            A.CallTo(() => result.InitForExecution(A<WorkflowExecutorResult>.Ignored, A<WorkflowDefinition>.Ignored, A<WorkflowInstance>.Ignored, A<ExecutionPointer>.Ignored)).Returns(ExecutionPipelineDirective.Next);
            A.CallTo(() => result.BeforeExecute(A<WorkflowExecutorResult>.Ignored, A<IStepExecutionContext>.Ignored, A<ExecutionPointer>.Ignored, A<IStepBody>.Ignored)).Returns(ExecutionPipelineDirective.Next);
            return result;
        }

        public interface IStepWithProperties : IStepBody
        {
            int Property1 { get; set; }
            int Property2 { get; set; }
            int Property3 { get; set; }
            DataClass Property4 { get; set; }
        }

        public class DataClass
        {
            public int Value1 { get; set; }
            public int Value2 { get; set; }
            public int Value3 { get; set; }
            public object Value4 { get; set; }
        }

        public class DynamicDataClass
        {
            public Dictionary<string, int> Storage { get; set; } = new Dictionary<string, int>();

            public int this[string propertyName]
            {
                get => Storage[propertyName];
                set => Storage[propertyName] = value;
            }
        }
    }
}
