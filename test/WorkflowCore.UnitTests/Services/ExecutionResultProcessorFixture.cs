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
    public class ExecutionResultProcessorFixture
    {
        protected IExecutionResultProcessor Subject;
        protected IExecutionPointerFactory PointerFactory;
        protected IDateTimeProvider DateTimeProvider;
        protected ILifeCycleEventPublisher EventHub;
        protected ICollection<IWorkflowErrorHandler> ErrorHandlers;
        protected WorkflowOptions Options;

        public ExecutionResultProcessorFixture()
        {
            PointerFactory = A.Fake<IExecutionPointerFactory>();
            DateTimeProvider = A.Fake<IDateTimeProvider>();
            EventHub = A.Fake<ILifeCycleEventPublisher>();
            ErrorHandlers = new HashSet<IWorkflowErrorHandler>();

            Options = new WorkflowOptions(A.Fake<IServiceCollection>());

            A.CallTo(() => DateTimeProvider.Now).Returns(DateTime.Now);
            A.CallTo(() => DateTimeProvider.UtcNow).Returns(DateTime.UtcNow);

            //config logging
            var loggerFactory = new LoggerFactory();
            //loggerFactory.AddConsole(LogLevel.Debug);            

            Subject = new ExecutionResultProcessor(PointerFactory, DateTimeProvider, EventHub, ErrorHandlers, Options, loggerFactory);
        }

        [Fact(DisplayName = "Should advance workflow")]
        public void should_advance_workflow()
        {
            //arrange            
            var definition = new WorkflowDefinition();
            var pointer1 = new ExecutionPointer() { Id = "1", Active = true, StepId = 0, Status = PointerStatus.Running };
            var pointer2 = new ExecutionPointer() { Id = "2" };
            var outcome = new StepOutcome() { NextStep = 1 };
            var step = A.Fake<WorkflowStep>();            
            var workflowResult = new WorkflowExecutorResult();
            var instance = GivenWorkflow(pointer1);
            var result = ExecutionResult.Next();

            A.CallTo(() => step.Outcomes).Returns(new List<StepOutcome>() { outcome });
            A.CallTo(() => PointerFactory.BuildNextPointer(definition, pointer1, outcome)).Returns(pointer2);

            //act
            Subject.ProcessExecutionResult(instance, definition, pointer1, step, result, workflowResult);

            //assert
            pointer1.Active.Should().BeFalse();
            pointer1.Status.Should().Be(PointerStatus.Complete);
            pointer1.EndTime.Should().NotBeNull();

            A.CallTo(() => PointerFactory.BuildNextPointer(definition, pointer1, outcome)).MustHaveHappened();
            instance.ExecutionPointers.Should().Contain(pointer2);
        }

        [Fact(DisplayName = "Should set persistence data")]
        public void should_set_persistence_data()
        {
            //arrange
            var persistenceData = new object();
            var definition = new WorkflowDefinition();
            var pointer = new ExecutionPointer() { Id = "1", Active = true, StepId = 0, Status = PointerStatus.Running };
            var step = A.Fake<WorkflowStep>();
            var workflowResult = new WorkflowExecutorResult();
            var instance = GivenWorkflow(pointer);
            var result = ExecutionResult.Persist(persistenceData);

            //act
            Subject.ProcessExecutionResult(instance, definition, pointer, step, result, workflowResult);

            //assert
            pointer.PersistenceData.Should().Be(persistenceData);
        }

        [Fact(DisplayName = "Should subscribe to event")]
        public void should_subscribe_to_event()
        {
            //arrange
            var definition = new WorkflowDefinition();
            var pointer = new ExecutionPointer() { Id = "1", Active = true, StepId = 0, Status = PointerStatus.Running };
            var step = A.Fake<WorkflowStep>();
            var workflowResult = new WorkflowExecutorResult();
            var instance = GivenWorkflow(pointer);
            var result = ExecutionResult.WaitForEvent("Event", "Key", DateTime.Now);

            //act
            Subject.ProcessExecutionResult(instance, definition, pointer, step, result, workflowResult);

            //assert
            pointer.Status.Should().Be(PointerStatus.WaitingForEvent);
            pointer.Active.Should().BeFalse();
            pointer.EventName.Should().Be("Event");
            pointer.EventKey.Should().Be("Key");
            workflowResult.Subscriptions.Should().Contain(x => x.StepId == pointer.StepId && x.EventName == "Event" && x.EventKey == "Key");
        }

        [Fact(DisplayName = "Should select correct outcomes")]
        public void should_select_correct_outcomes()
        {
            //arrange            
            var definition = new WorkflowDefinition();
            var pointer1 = new ExecutionPointer() { Id = "1", Active = true, StepId = 0, Status = PointerStatus.Running };
            var pointer2 = new ExecutionPointer() { Id = "2" };
            var pointer3 = new ExecutionPointer() { Id = "3" };
            var outcome1 = new StepOutcome() { NextStep = 1, Value = data => 10 };
            var outcome2 = new StepOutcome() { NextStep = 2, Value = data => 20 };
            var step = A.Fake<WorkflowStep>();
            var workflowResult = new WorkflowExecutorResult();
            var instance = GivenWorkflow(pointer1);
            var result = ExecutionResult.Outcome(20);

            A.CallTo(() => step.Outcomes).Returns(new List<StepOutcome>() { outcome1, outcome2 });
            A.CallTo(() => PointerFactory.BuildNextPointer(definition, pointer1, outcome1)).Returns(pointer2);
            A.CallTo(() => PointerFactory.BuildNextPointer(definition, pointer1, outcome2)).Returns(pointer3);

            //act
            Subject.ProcessExecutionResult(instance, definition, pointer1, step, result, workflowResult);

            //assert
            pointer1.Active.Should().BeFalse();
            pointer1.Status.Should().Be(PointerStatus.Complete);
            pointer1.EndTime.Should().NotBeNull();

            A.CallTo(() => PointerFactory.BuildNextPointer(definition, pointer1, outcome1)).MustNotHaveHappened();
            A.CallTo(() => PointerFactory.BuildNextPointer(definition, pointer1, outcome2)).MustHaveHappened();
            instance.ExecutionPointers.Should().NotContain(pointer2);
            instance.ExecutionPointers.Should().Contain(pointer3);
        }

        [Fact(DisplayName = "Should sleep pointer")]
        public void should_sleep_pointer()
        {
            //arrange
            var persistenceData = new object();
            var definition = new WorkflowDefinition();
            var pointer = new ExecutionPointer() { Id = "1", Active = true, StepId = 0, Status = PointerStatus.Running };
            var step = A.Fake<WorkflowStep>();            
            var workflowResult = new WorkflowExecutorResult();
            var instance = GivenWorkflow(pointer);
            var result = ExecutionResult.Sleep(TimeSpan.FromMinutes(5), persistenceData);

            //act
            Subject.ProcessExecutionResult(instance, definition, pointer, step, result, workflowResult);

            //assert
            pointer.Status.Should().Be(PointerStatus.Sleeping);
            pointer.SleepUntil.Should().NotBeNull();
        }

        [Fact(DisplayName = "Should branch children")]
        public void should_branch_children()
        {
            //arrange
            var branch = 10;
            var child = 2;
            var definition = new WorkflowDefinition();
            var pointer = new ExecutionPointer() { Id = "1", Active = true, StepId = 0, Status = PointerStatus.Running };
            var childPointer = new ExecutionPointer() { Id = "2" };
            var step = A.Fake<WorkflowStep>();
            var workflowResult = new WorkflowExecutorResult();
            var instance = GivenWorkflow(pointer);
            var result = ExecutionResult.Branch(new List<object>() { branch }, null);

            A.CallTo(() => step.Children).Returns(new List<int>() { child });
            A.CallTo(() => PointerFactory.BuildChildPointer(definition, pointer, child, branch)).Returns(childPointer);

            //act
            Subject.ProcessExecutionResult(instance, definition, pointer, step, result, workflowResult);

            //assert
            A.CallTo(() => PointerFactory.BuildChildPointer(definition, pointer, child, branch)).MustHaveHappened();
            instance.ExecutionPointers.Should().Contain(childPointer);
        }
        
        private static WorkflowInstance GivenWorkflow(ExecutionPointer pointer)
        {
            return new WorkflowInstance
            {
                Status = WorkflowStatus.Runnable,
                ExecutionPointers = new ExecutionPointerCollection(new List<ExecutionPointer>()
                {
                    pointer
                })
            };
        }
    }
}
