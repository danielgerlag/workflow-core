using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;
using FluentAssertions;
using Xunit;
using System.Linq.Expressions;

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
            EventHub = A.Fake<ILifeCycleEventPublisher>();
            DateTimeProvider = A.Fake<IDateTimeProvider>();

            Options = new WorkflowOptions(A.Fake<IServiceCollection>());

            A.CallTo(() => DateTimeProvider.Now).Returns(DateTime.Now);

            //config logging
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole(LogLevel.Debug);

            Subject = new WorkflowExecutor(Registry, ServiceProvider, DateTimeProvider, ResultProcesser, EventHub, Options, loggerFactory);
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
            Expression<Func<IStepWithProperties, int>> p = x => x.Int;
            Expression<Func<DataClass, IStepExecutionContext, int>> v = (x, context) => x.Int;

            var step1Body = A.Fake<IStepWithProperties>();
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Returns(ExecutionResult.Next());
            WorkflowStep step1 = BuildFakeStep(step1Body, new List<DataMapping>()
                {
                    new DataMapping()
                    {
                        Source = v,
                        Target = p
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
                Data = new DataClass() { Int = 5 },
                ExecutionPointers = new List<ExecutionPointer>()
                {
                    new ExecutionPointer() { Active = true, StepId = 0 }
                }
            };

            //act
            Subject.Execute(instance);

            //assert
            step1Body.Int.Should().Be(5);
        }

        [Fact(DisplayName = "Should map value outputs")]
        public void should_map_value_type_outputs()
        {
            //arrange
            Expression<Func<IStepWithProperties, int>> p = x => x.Int;
            Expression<Func<DataClass, IStepExecutionContext, int>> v = (x, context) => x.Int;

            var step1Body = A.Fake<IStepWithProperties>();
            const int @int = 7;
            A.CallTo(() => step1Body.Int).Returns(@int);
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Returns(ExecutionResult.Next());
            WorkflowStep step1 = BuildFakeStep(step1Body, new List<DataMapping>(), new List<DataMapping>()
                {
                    new DataMapping()
                    {
                        Source = p,
                        Target = v
                    }
                }
            );

            Given1StepWorkflow(step1, "Workflow", 1);

            var data = new DataClass() { Int = 5 };

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
            data.Int.Should().Be(@int);
        }

        [Theory(DisplayName = "Should map nullable value outputs")]
        [InlineData(7)]
        [InlineData(null)]
        public void should_map_nullable_value_type_outputs(int? nullableInt)
        {
            //arrange
            Expression<Func<IStepWithProperties, int?>> p = x => x.NullableInt;
            Expression<Func<DataClass, IStepExecutionContext, int?>> v = (x, context) => x.NullableInt;

            var step1Body = A.Fake<IStepWithProperties>();
            A.CallTo(() => step1Body.NullableInt).Returns(nullableInt);
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Returns(ExecutionResult.Next());
            WorkflowStep step1 = BuildFakeStep(step1Body, new List<DataMapping>(), new List<DataMapping>()
                {
                    new DataMapping()
                    {
                        Source = p,
                        Target = v
                    }
                }
            );

            Given1StepWorkflow(step1, "Workflow", 1);

            var data = new DataClass() { NullableInt = 5 };

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
            data.NullableInt.Should().Be(nullableInt);
        }

        [Fact(DisplayName = "Should map enum value outputs")]
        public void should_map_enum_outputs()
        {
            //arrange
            Expression<Func<IStepWithProperties, int>> p = x => x.EnumInt;
            Expression<Func<DataClass, IStepExecutionContext, Enum>> v = (x, context) => x.Enum;

            var step1Body = A.Fake<IStepWithProperties>();
            const int enumInt = 2;
            A.CallTo(() => step1Body.EnumInt).Returns(enumInt);
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Returns(ExecutionResult.Next());
            WorkflowStep step1 = BuildFakeStep(step1Body, new List<DataMapping>(), new List<DataMapping>()
                {
                    new DataMapping
                    {
                        Source = p,
                        Target = v
                    },
                }
            );

            Given1StepWorkflow(step1, "Workflow", 1);

            var data = new DataClass
            {
                Enum = Enum.One,
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
            data.Enum.Should().Be(Enum.Two);
        }

        [Fact(DisplayName = "Should map nullable enum value outputs")]
        public void should_map_nullable_enum_outputs()
        {
            //arrange
            Expression<Func<IStepWithProperties, int>> p = x => x.EnumInt;
            Expression<Func<DataClass, IStepExecutionContext, Enum?>> v = (x, context) => x.NullableEnum;

            var step1Body = A.Fake<IStepWithProperties>();
            const int enumInt = 2;
            A.CallTo(() => step1Body.EnumInt).Returns(enumInt);
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Returns(ExecutionResult.Next());
            WorkflowStep step1 = BuildFakeStep(step1Body, new List<DataMapping>(), new List<DataMapping>()
                {
                    new DataMapping
                    {
                        Source = p,
                        Target = v
                    },
                }
            );

            Given1StepWorkflow(step1, "Workflow", 1);

            var data = new DataClass
            {
                NullableEnum = Enum.One,
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
            data.NullableEnum.Should().Be(Enum.Two);
        }

        [Fact(DisplayName = "Should map dynamic outputs")]
        public void should_map_dynamic_outputs()
        {
            //arrange
            Expression<Func<IStepWithProperties, int>> p = x => x.Int;
            Expression<Func<DynamicDataClass, IStepExecutionContext, int>> v = (x, context) => x["Value1"];

            var step1Body = A.Fake<IStepWithProperties>();
            A.CallTo(() => step1Body.Int).Returns(7);
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Returns(ExecutionResult.Next());
            WorkflowStep step1 = BuildFakeStep(step1Body, new List<DataMapping>(), new List<DataMapping>()
                {
                    new DataMapping()
                    {
                        Source = p,
                        Target = v
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

        [Fact(DisplayName = "Should map class reference outputs")]
        public void should_map_class_reference_outputs()
        {
            //arrange
            Expression<Func<IStepWithProperties, Class1>> p = x => x.Class;
            Expression<Func<DataClass, IStepExecutionContext, Class1>> v = (x, context) => x.Class;

            var step1Body = A.Fake<IStepWithProperties>();
            const int @int = 2;
            A.CallTo(() => step1Body.Class).Returns(new Class2
            {
                Int = @int,
            });
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Returns(ExecutionResult.Next());
            WorkflowStep step1 = BuildFakeStep(step1Body, new List<DataMapping>(), new List<DataMapping>()
                {
                    new DataMapping()
                    {
                        Source = p,
                        Target = v
                    }
                }
            );

            Given1StepWorkflow(step1, "Workflow", 1);

            var data = new DataClass
            {
                Class = new Class1
                {
                    Int = 1,
                }
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
            data.Class.Should().BeOfType<Class2>();
            data.Class.Int.Should().Be(@int);
        }

        [Fact(DisplayName = "Should map interface reference outputs")]
        public void should_map_interface_reference_outputs()
        {
            //arrange
            Expression<Func<IStepWithProperties, IInterface>> p = x => x.Interface;
            Expression<Func<DataClass, IStepExecutionContext, IInterface>> v = (x, context) => x.Interface;

            var step1Body = A.Fake<IStepWithProperties>();
            var @int = 2;
            A.CallTo(() => step1Body.Interface).Returns(new Class2
            {
                Int = @int,
            });
            A.CallTo(() => step1Body.RunAsync(A<IStepExecutionContext>.Ignored)).Returns(ExecutionResult.Next());
            WorkflowStep step1 = BuildFakeStep(step1Body, new List<DataMapping>(), new List<DataMapping>()
                {
                    new DataMapping()
                    {
                        Source = p,
                        Target = v
                    }
                }
            );

            Given1StepWorkflow(step1, "Workflow", 1);

            var data = new DataClass
            {
                Interface = new Class1
                {
                    Int = 1,
                }
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
            data.Interface.Should().BeOfType<Class2>();
            data.Interface.Int.Should().Be(@int);
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

        public interface IInterface
        {
            int Int { get; set; }
        }

        public class Class1 : IInterface
        {
            public int Int { get; set; }
        }

        public class Class2 : Class1 { }

        public enum Enum
        {
            One = 1,
            Two = 2,
        }

        public interface IStepWithProperties : IStepBody
        {
            int Int { get; set; }
            int? NullableInt { get; set; }
            Enum Enum { get; set; }
            int EnumInt { get; set; }
            IInterface Interface { get; set; }
            Class1 Class { get; set; }
        }

        public class DataClass
        {
            public int Int { get; set; }
            public int? NullableInt { get; set; }
            public Enum Enum { get; set; }
            public Enum? NullableEnum { get; set; }
            public IInterface Interface { get; set; }
            public Class1 Class { get; set; }
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
