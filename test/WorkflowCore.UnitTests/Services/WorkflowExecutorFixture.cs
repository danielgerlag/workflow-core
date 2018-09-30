using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;
using FluentAssertions;
using Xunit;
using WorkflowCore.Primitives;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace WorkflowCore.UnitTests.Services
{
    public class WorkflowExecutorFixture
    {
        protected IWorkflowExecutor Subject;
        protected IWorkflowHost Host;
        protected IPersistenceProvider PersistenceProvider;
        protected IWorkflowRegistry Registry;
        protected IExecutionResultProcessor ResultProcesser;
        protected IServiceProvider ServiceProvider;
        protected IDateTimeProvider DateTimeProvider;
        protected WorkflowOptions Options;

        public WorkflowExecutorFixture()
        {
            Host = A.Fake<IWorkflowHost>();
            PersistenceProvider = A.Fake<IPersistenceProvider>();
            ServiceProvider = A.Fake<IServiceProvider>();
            Registry = A.Fake<IWorkflowRegistry>();
            ResultProcesser = A.Fake<IExecutionResultProcessor>();
            DateTimeProvider = A.Fake<IDateTimeProvider>();

            Options = new WorkflowOptions(A.Fake<IServiceCollection>());

            A.CallTo(() => DateTimeProvider.Now).Returns(DateTime.Now);

            //config logging
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole(LogLevel.Debug);            

            Subject = new WorkflowExecutor(Registry, ServiceProvider, DateTimeProvider, ResultProcesser, Options, loggerFactory);
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
                ExecutionPointers = new List<ExecutionPointer>()
                {
                    new ExecutionPointer() { Active = true, StepId = 0 }
                }
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
                ExecutionPointers = new List<ExecutionPointer>()
                {
                    new ExecutionPointer() { Active = true, StepId = 0 }
                }
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
                ExecutionPointers = new List<ExecutionPointer>()
                {
                    new ExecutionPointer() { Active = false, StepId = 0 }
                }
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
            Expression<Func<IStepWithProperties, int>> p1 = x => x.Property1;
            Expression<Func<DataClass, IStepExecutionContext, int>> v1 = (x, context) => x.Value1;

            var step1Body = A.Fake<IStepWithProperties>();            
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Returns(ExecutionResult.Next());
            WorkflowStep step1 = BuildFakeStep(step1Body, new List<DataMapping>()
                {
                    new DataMapping()
                    {
                        Source = v1,
                        Target = p1
                    }
                }
            , new List<DataMapping>());

            Given1StepWorkflow(step1, "Workflow", 1);

            var instance = new WorkflowInstance
            {
                WorkflowDefinitionId = "Workflow",
                Version = 1,
                Status = WorkflowStatus.Runnable,
                NextExecution = 0,
                Id = "001",
                Data = new DataClass() { Value1 = 5 },
                ExecutionPointers = new List<ExecutionPointer>()
                {
                    new ExecutionPointer() { Active = true, StepId = 0 }
                }
            };

            //act
            Subject.Execute(instance);

            //assert
            step1Body.Property1.Should().Be(5);
        }

        [Fact(DisplayName = "Should map outputs")]
        public void should_map_outputs()
        {
            //arrange
            Expression<Func<IStepWithProperties, int>> p1 = x => x.Property1;
            Expression<Func<DataClass, IStepExecutionContext, int>> v1 = (x, context) => x.Value1;

            var step1Body = A.Fake<IStepWithProperties>();
            A.CallTo(() => step1Body.Property1).Returns(7);
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Returns(ExecutionResult.Next());
            WorkflowStep step1 = BuildFakeStep(step1Body, new List<DataMapping>(), new List<DataMapping>()
                {
                    new DataMapping()
                    {
                        Source = p1,
                        Target = v1
                    }
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
                ExecutionPointers = new List<ExecutionPointer>()
                {
                    new ExecutionPointer() { Active = true, StepId = 0 }
                }
            };
            
            //act
            Subject.Execute(instance);

            //assert
            data.Value1.Should().Be(7);
        }

        [Fact(DisplayName = "Should map dynamic outputs")]
        public void should_map_outputs_dynamic()
        {
            //arrange
            Expression<Func<IStepWithProperties, int>> p1 = x => x.Property1;
            Expression<Func<DynamicDataClass, IStepExecutionContext, int>> v1 = (x, context) => x["Value1"];

            var step1Body = A.Fake<IStepWithProperties>();
            A.CallTo(() => step1Body.Property1).Returns(7);
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Returns(ExecutionResult.Next());
            WorkflowStep step1 = BuildFakeStep(step1Body, new List<DataMapping>(), new List<DataMapping>()
                {
                    new DataMapping()
                    {
                        Source = p1,
                        Target = v1
                    }
                }
            );

            Given1StepWorkflow(step1, "Workflow", 1);

            var data = new DynamicDataClass()
            {
                ["Value1"] = 5
            };

            var instance = new WorkflowInstance
            {
                WorkflowDefinitionId = "Workflow",
                Version = 1,
                Status = WorkflowStatus.Runnable,
                NextExecution = 0,
                Id = "001",
                Data = data,
                ExecutionPointers = new List<ExecutionPointer>()
                {
                    new ExecutionPointer() { Active = true, StepId = 0 }
                }
            };

            //act
            Subject.Execute(instance);

            //assert
            data["Value1"].Should().Be(7);
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
                ExecutionPointers = new List<ExecutionPointer>()
                {
                    new ExecutionPointer() { Active = true, StepId = 0 }
                }
            };

            //act
            Subject.Execute(instance);

            //assert
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).MustHaveHappened();
            A.CallTo(() => ResultProcesser.HandleStepException(instance, A<WorkflowDefinition>.Ignored, A<ExecutionPointer>.Ignored, step1)).MustHaveHappened();
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
                ExecutionPointers = new List<ExecutionPointer>()
                {
                    new ExecutionPointer() { Active = true, StepId = 0 }
                }
            };

            //act
            Subject.Execute(instance);

            //assert
            A.CallTo(() => step1.AfterWorkflowIteration(A<WorkflowExecutorResult>.Ignored, A<WorkflowDefinition>.Ignored, instance, A<ExecutionPointer>.Ignored)).MustHaveHappened();
        }


        private void Given1StepWorkflow(WorkflowStep step1, string id, int version)
        {
            A.CallTo(() => Registry.GetDefinition(id, version)).Returns(new WorkflowDefinition()
            {
                Id = id,
                Version = version,
                DataType = typeof(object),
                Steps = new List<WorkflowStep>()
                {
                    step1
                }

            });
        }

        private WorkflowStep BuildFakeStep(IStepBody stepBody)
        {
            return BuildFakeStep(stepBody, new List<DataMapping>(), new List<DataMapping>());
        }

        private WorkflowStep BuildFakeStep(IStepBody stepBody, List<DataMapping> inputs, List<DataMapping> outputs)
        {
            var result = A.Fake<WorkflowStep>();
            A.CallTo(() => result.Id).Returns(0);
            A.CallTo(() => result.BodyType).Returns(stepBody.GetType());
            A.CallTo(() => result.ResumeChildrenAfterCompensation).Returns(true);
            A.CallTo(() => result.RevertChildrenAfterCompensation).Returns(false);
            A.CallTo(() => result.ConstructBody(ServiceProvider)).Returns(stepBody);
            A.CallTo(() => result.Inputs).Returns(inputs);
            A.CallTo(() => result.Outputs).Returns(outputs);
            A.CallTo(() => result.Outcomes).Returns(new List<StepOutcome>());
            A.CallTo(() => result.InitForExecution(A<WorkflowExecutorResult>.Ignored, A<WorkflowDefinition>.Ignored, A<WorkflowInstance>.Ignored, A<ExecutionPointer>.Ignored)).Returns(ExecutionPipelineDirective.Next);
            A.CallTo(() => result.BeforeExecute(A<WorkflowExecutorResult>.Ignored, A<IStepExecutionContext>.Ignored, A<ExecutionPointer>.Ignored, A<IStepBody>.Ignored)).Returns(ExecutionPipelineDirective.Next);
            return result;
        }

        public interface IStepWithProperties : IStepBody
        {
            int Property1 { get; set; }
            int Property2 { get; set; }
            int Property3 { get; set; }            
        }

        public class DataClass
        {
            public int Value1 { get; set; }
            public int Value2 { get; set; }
            public int Value3 { get; set; }
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
