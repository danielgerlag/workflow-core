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

namespace WorkflowCore.UnitTests.Services
{
    public class WorkflowExecutorFixture
    {
        class EventSubscribeTestWorkflow : IWorkflow
        {
            static int StartStepTicker = 0;
            public string Id { get { return "EventSubscribeTestWorkflow"; } }
            public int Version { get { return 1; } }
            public void Build(IWorkflowBuilder<object> builder)
            {
                builder
                    .StartWith(context =>
                    {
                        StartStepTicker++;
                        return ExecutionResult.Next();
                    })
                    .WaitFor("MyEvent", data => "0");
            }

        }

        class StepExecutionTestWorkflow : IWorkflow
        {
            public static int Step1StepTicker = 0;
            public static int Step2StepTicker = 0;
            public string Id { get { return "StepExecutionTestWorkflow"; } }
            public int Version { get { return 1; } }
            public void Build(IWorkflowBuilder<object> builder)
            {
                builder
                    .StartWith(context =>
                    {
                        Step1StepTicker++;
                        return ExecutionResult.Next();
                    })
                    .Then(context =>
                    {
                        Step2StepTicker++;
                        return ExecutionResult.Next();
                    });
            }
        }

        protected IWorkflowExecutor Subject;
        protected IWorkflowHost Host;
        protected IPersistenceProvider PersistenceProvider;
        protected IWorkflowRegistry Registry;
        protected WorkflowOptions Options;

        public WorkflowExecutorFixture()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();

            Options = new WorkflowOptions();
            services.AddTransient<IWorkflowBuilder, WorkflowBuilder>();
            services.AddTransient<IWorkflowRegistry, WorkflowRegistry>();
            
            Host = A.Fake<IWorkflowHost>();
            PersistenceProvider = A.Fake<IPersistenceProvider>();
            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddConsole(LogLevel.Debug);

            Registry = serviceProvider.GetService<IWorkflowRegistry>();

            Subject = new WorkflowExecutor(Registry, serviceProvider, new DateTimeProvider(), loggerFactory);            
        }

        [Fact]
        public void EventSubscribe()
        {
            //arrange
            var def = new EventSubscribeTestWorkflow();
            Registry.RegisterWorkflow(def);

            var instance = new WorkflowInstance();
            instance.WorkflowDefinitionId = def.Id;
            instance.Version = def.Version;
            instance.Status = WorkflowStatus.Runnable;
            instance.NextExecution = 0;
            instance.Id = "001";

            var executionPointer = new ExecutionPointer()
            {
                Active = true,
                StepId = 1
            };

            instance.ExecutionPointers.Add(executionPointer);

            //act
            Subject.Execute(instance, Options);

            //assert
            executionPointer.EventName.Should().Be("MyEvent");
            executionPointer.EventKey.Should().Be("0");
            executionPointer.Active.Should().Be(false);
        }

        [Fact]
        public void StepExecution()
        {
            //arrange
            var def = new StepExecutionTestWorkflow();
            Registry.RegisterWorkflow(def);

            var instance = new WorkflowInstance();
            instance.WorkflowDefinitionId = def.Id;
            instance.Version = def.Version;
            instance.Status = WorkflowStatus.Runnable;
            instance.NextExecution = 0;
            instance.Id = "001";

            instance.ExecutionPointers.Add(new ExecutionPointer()
            {
                Active = true,
                StepId = 0
            });                        

            //act
            Subject.Execute(instance, Options);
            Subject.Execute(instance, Options);

            //assert
            StepExecutionTestWorkflow.Step1StepTicker.Should().Be(1);
            StepExecutionTestWorkflow.Step2StepTicker.Should().Be(1);
            instance.Status.Should().Be(WorkflowStatus.Complete);            
        }
    }
}
