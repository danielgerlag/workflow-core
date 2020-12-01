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

namespace WorkflowCore.UnitTests.Services
{
    public class WorkflowExecutorFixture
    {
        protected IWorkflowExecutor Subject;
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
        protected IWorkflowMiddlewareRunner MiddlewareRunner;
        protected WorkflowOptions Options;

        public WorkflowExecutorFixture()
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

            Options = new WorkflowOptions(A.Fake<IServiceCollection>());

            var scope = A.Fake<IServiceScope>();
            A.CallTo(() => ScopeProvider.CreateScope(A<IStepExecutionContext>._)).Returns(scope);
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

            A.CallTo(() => StepExecutor.ExecuteStep(A<IStepExecutionContext>._, A<IStepBody>._))
                .ReturnsLazily(call =>
                    call.Arguments[1].As<IStepBody>().RunAsync(
                        call.Arguments[0].As<IStepExecutionContext>()));

            //config logging
            var loggerFactory = new LoggerFactory();
            //loggerFactory.AddConsole(LogLevel.Debug);

            Subject = new WorkflowExecutor(Registry, ServiceProvider, ScopeProvider, DateTimeProvider, ResultProcesser, EventHub, CancellationProcessor, Options, loggerFactory);
        }

        [Fact(DisplayName = "Should execute active step")]
        public void should_execute_active_step()
        {
            //arrange
            var step1Body = A.Fake<IStepBody>();
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Returns(ExecutionResult.Next());
            WorkflowStep step1 = BuildFakeStep(step1Body);
            Given1StepWorkflow(step1, "Workflow", 1);

            var instance = new WorkflowInstance
            {
                WorkflowDefinitionId = "Workflow",
                Version = 1,
                Status = WorkflowStatus.Runnable,
                NextExecution = 0,
                Id = "001",
                ExecutionPointers = new ExecutionPointerCollection(new List<ExecutionPointer>()
                {
                    new ExecutionPointer() { Id = "1", Active = true, StepId = 0 }
                })
            };

            //act
            Subject.Execute(instance);

            //assert
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).MustHaveHappened();
            A.CallTo(() => ResultProcesser.ProcessExecutionResult(instance, A<WorkflowDefinition>.Ignored, A<ExecutionPointer>.Ignored, step1, A<ExecutionResult>.Ignored, A<WorkflowExecutorResult>.Ignored)).MustHaveHappened();
        }

        [Fact(DisplayName = "Should trigger step hooks")]
        public void should_trigger_step_hooks()
        {
            //arrange
            var step1Body = A.Fake<IStepBody>();
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Returns(ExecutionResult.Next());
            WorkflowStep step1 = BuildFakeStep(step1Body);
            Given1StepWorkflow(step1, "Workflow", 1);

            var instance = new WorkflowInstance
            {
                WorkflowDefinitionId = "Workflow",
                Version = 1,
                Status = WorkflowStatus.Runnable,
                NextExecution = 0,
                Id = "001",
                ExecutionPointers = new ExecutionPointerCollection(new List<ExecutionPointer>()
                {
                    new ExecutionPointer() { Id = "1", Active = true, StepId = 0 }
                })
            };

            //act
            Subject.Execute(instance);

            //assert
            A.CallTo(() => step1.InitForExecution(A<WorkflowExecutorResult>.Ignored, A<WorkflowDefinition>.Ignored, A<WorkflowInstance>.Ignored, A<ExecutionPointer>.Ignored)).MustHaveHappened();
            A.CallTo(() => step1.BeforeExecute(A<WorkflowExecutorResult>.Ignored, A<IStepExecutionContext>.Ignored, A<ExecutionPointer>.Ignored, A<IStepBody>.Ignored)).MustHaveHappened();
            A.CallTo(() => step1.AfterExecute(A<WorkflowExecutorResult>.Ignored, A<IStepExecutionContext>.Ignored, A<ExecutionResult>.Ignored, A<ExecutionPointer>.Ignored)).MustHaveHappened();
        }

        [Fact(DisplayName = "Should not execute inactive step")]
        public void should_not_execute_inactive_step()
        {
            //arrange
            var step1Body = A.Fake<IStepBody>();
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Returns(ExecutionResult.Next());
            WorkflowStep step1 = BuildFakeStep(step1Body);
            Given1StepWorkflow(step1, "Workflow", 1);

            var instance = new WorkflowInstance
            {
                WorkflowDefinitionId = "Workflow",
                Version = 1,
                Status = WorkflowStatus.Runnable,
                NextExecution = 0,
                Id = "001",
                ExecutionPointers = new ExecutionPointerCollection(new List<ExecutionPointer>()
                {
                    new ExecutionPointer() { Id = "1", Active = false, StepId = 0 }
                })
            };

            //act
            Subject.Execute(instance);

            //assert
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).MustNotHaveHappened();
        }

        [Fact(DisplayName = "Should map inputs")]
        public void should_map_inputs()
        {
            //arrange
            var param = A.Fake<IStepParameter>();

            var step1Body = A.Fake<IStepWithProperties>();
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Returns(ExecutionResult.Next());
            WorkflowStep step1 = BuildFakeStep(step1Body, new List<IStepParameter>()
                {
                    param
                }
            , new List<IStepParameter>());

            Given1StepWorkflow(step1, "Workflow", 1);

            var instance = new WorkflowInstance
            {
                WorkflowDefinitionId = "Workflow",
                Version = 1,
                Status = WorkflowStatus.Runnable,
                NextExecution = 0,
                Id = "001",
                Data = new DataClass() { Value1 = 5 },
                ExecutionPointers = new ExecutionPointerCollection(new List<ExecutionPointer>()
                {
                    new ExecutionPointer() { Id = "1", Active = true, StepId = 0 }
                })
            };

            //act
            Subject.Execute(instance);

            //assert
            A.CallTo(() => param.AssignInput(A<object>.Ignored, step1Body, A<IStepExecutionContext>.Ignored))
                .MustHaveHappened();
        }

        [Fact(DisplayName = "Should map outputs")]
        public void should_map_outputs()
        {
            //arrange
            var param = A.Fake<IStepParameter>();

            var step1Body = A.Fake<IStepWithProperties>();
            A.CallTo(() => step1Body.Property1).Returns(7);
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Returns(ExecutionResult.Next());
            WorkflowStep step1 = BuildFakeStep(step1Body, new List<IStepParameter>(), new List<IStepParameter>()
                {
                    param
                }
            );

            Given1StepWorkflow(step1, "Workflow", 1);

            var data = new DataClass() { Value1 = 5 };

            var instance = new WorkflowInstance
            {
                WorkflowDefinitionId = "Workflow",
                Version = 1,
                Status = WorkflowStatus.Runnable,
                NextExecution = 0,
                Id = "001",
                Data = data,
                ExecutionPointers = new ExecutionPointerCollection(new List<ExecutionPointer>()
                {
                    new ExecutionPointer() { Id = "1", Active = true, StepId = 0 }
                })
            };

            //act
            Subject.Execute(instance);

            //assert
            A.CallTo(() => param.AssignOutput(data, step1Body, A<IStepExecutionContext>.Ignored))
                .MustHaveHappened();
        }



        [Fact(DisplayName = "Should handle step exception")]
        public void should_handle_step_exception()
        {
            //arrange
            var step1Body = A.Fake<IStepBody>();
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Throws<Exception>();
            WorkflowStep step1 = BuildFakeStep(step1Body);
            Given1StepWorkflow(step1, "Workflow", 1);

            var instance = new WorkflowInstance
            {
                WorkflowDefinitionId = "Workflow",
                Version = 1,
                Status = WorkflowStatus.Runnable,
                NextExecution = 0,
                Id = "001",
                ExecutionPointers = new ExecutionPointerCollection(new List<ExecutionPointer>()
                {
                    new ExecutionPointer() { Id = "1", Active = true, StepId = 0 }
                })
            };

            //act
            Subject.Execute(instance);

            //assert
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).MustHaveHappened();
            A.CallTo(() => ResultProcesser.HandleStepException(instance, A<WorkflowDefinition>.Ignored, A<ExecutionPointer>.Ignored, step1, A<Exception>.Ignored)).MustHaveHappened();
            A.CallTo(() => ResultProcesser.ProcessExecutionResult(instance, A<WorkflowDefinition>.Ignored, A<ExecutionPointer>.Ignored, step1, A<ExecutionResult>.Ignored, A<WorkflowExecutorResult>.Ignored)).MustNotHaveHappened();
        }

        [Fact(DisplayName = "Should process after execution iteration")]
        public void should_process_after_execution_iteration()
        {
            //arrange
            var step1Body = A.Fake<IStepBody>();
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Returns(ExecutionResult.Persist(null));
            WorkflowStep step1 = BuildFakeStep(step1Body);
            Given1StepWorkflow(step1, "Workflow", 1);

            var instance = new WorkflowInstance
            {
                WorkflowDefinitionId = "Workflow",
                Version = 1,
                Status = WorkflowStatus.Runnable,
                NextExecution = 0,
                Id = "001",
                ExecutionPointers = new ExecutionPointerCollection(new List<ExecutionPointer>()
                {
                    new ExecutionPointer() { Id = "1", Active = true, StepId = 0 }
                })
            };

            //act
            Subject.Execute(instance);

            //assert
            A.CallTo(() => step1.AfterWorkflowIteration(A<WorkflowExecutorResult>.Ignored, A<WorkflowDefinition>.Ignored, instance, A<ExecutionPointer>.Ignored)).MustHaveHappened();
        }

        [Fact(DisplayName = "Should process cancellations")]
        public void should_process_cancellations()
        {
            //arrange
            var step1Body = A.Fake<IStepBody>();
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Returns(ExecutionResult.Persist(null));
            WorkflowStep step1 = BuildFakeStep(step1Body);
            Given1StepWorkflow(step1, "Workflow", 1);

            var instance = new WorkflowInstance
            {
                WorkflowDefinitionId = "Workflow",
                Version = 1,
                Status = WorkflowStatus.Runnable,
                NextExecution = 0,
                Id = "001",
                ExecutionPointers = new ExecutionPointerCollection(new List<ExecutionPointer>()
                {
                    new ExecutionPointer() { Id = "1", Active = true, StepId = 0 }
                })
            };

            //act
            Subject.Execute(instance);

            //assert
            A.CallTo(() => CancellationProcessor.ProcessCancellations(instance, A<WorkflowDefinition>.Ignored, A<WorkflowExecutorResult>.Ignored)).MustHaveHappened();
        }


        private void Given1StepWorkflow(WorkflowStep step1, string id, int version)
        {
            A.CallTo(() => Registry.GetDefinition(id, version)).Returns(new WorkflowDefinition()
            {
                Id = id,
                Version = version,
                DataType = typeof(object),
                Steps = new WorkflowStepCollection()
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
