using System;
using System.Collections.Generic;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;
using WorkflowCore.Primitives;
using WorkflowCore.Services.ErrorHandlers;
using Xunit;

namespace WorkflowCore.UnitTests.Handlers
{
    public class CatchHandlerFixture
    {
        private ILifeCycleEventPublisher _eventPublisher;
        private IExecutionPointerFactory _pointerFactory;
        private IDateTimeProvider _datetimeProvider;
        private WorkflowOptions _options;
        private DateTime _now = DateTime.Now;
        
        private CatchHandler _subject;

        public CatchHandlerFixture()
        {
            _eventPublisher = A.Fake<ILifeCycleEventPublisher>();
            _pointerFactory = A.Fake<IExecutionPointerFactory>();
            _datetimeProvider = A.Fake<IDateTimeProvider>();
            A.CallTo(() => _datetimeProvider.Now).Returns(_now);
            
            _options = new WorkflowOptions(A.Fake<IServiceCollection>());
            _subject = new CatchHandler(_pointerFactory, _eventPublisher, _datetimeProvider, _options);
        }

        [Fact(DisplayName = "Should have type of Catch")]
        public void should_have_type_of_catch()
        {
            _subject.Type.Should().Be(WorkflowErrorHandling.Catch);
        }

        [Fact(DisplayName = "Should catch exception with one catch step")]
        public void should_catch_exception_with_one_catch_step()
        {
            //arrange
            var tryStepId = 123;
            var tryPointer = SetupTryPointer(tryStepId);
            var workflow = SetupWorkflow(new List<ExecutionPointer> {tryPointer});
            var tryStep = SetupTryStep(tryStepId);
            
            var catchStepId = 345;
            tryStep.CatchStepsQueue.Enqueue(new KeyValuePair<Type, int>(typeof(Exception), catchStepId));
            
            var definition = SetupWorkflowDefinition(tryStep);

            var exception = new ArgumentException("message");
            
            A.CallTo(() => _pointerFactory.BuildCatchPointer(A<WorkflowDefinition>._, A<ExecutionPointer>._,
                    A<ExecutionPointer>._, A<int>._, A<Exception>._))
                .Returns(new ExecutionPointer {Id = "catchPointerId"});
            
            //act
            _subject.Handle(workflow, definition, tryPointer, new WorkflowStepInline(), exception, new Queue<ExecutionPointer>());
            
            //assert
            tryPointer.Active.Should().BeFalse();
            tryPointer.EndTime.Should().Be(_now.ToUniversalTime());
            tryPointer.Status.Should().Be(PointerStatus.Failed);
            
            workflow.ExecutionPointers.Count.Should().Be(2);
            
            A.CallTo(() => _pointerFactory.BuildCatchPointer(A<WorkflowDefinition>._, A<ExecutionPointer>._,
                    A<ExecutionPointer>._, A<int>._, A<Exception>._))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _pointerFactory.BuildNextPointer(A<WorkflowDefinition>._, A<ExecutionPointer>._,
                A<IStepOutcome>._)).MustNotHaveHappened();
            
            workflow.Status.Should().NotBe(WorkflowStatus.Terminated);
            A.CallTo(() => _eventPublisher.PublishNotification(A<LifeCycleEvent>._))
                .MustNotHaveHappened();
        }

        [Fact(DisplayName = "Should catch exception with inner catch step")]
        public void should_catch_exception_with_inner_catch_step()
        {
            //arrange
            var tryStepId = 123;
            var tryPointer = SetupTryPointer(tryStepId);
            var workflow = SetupWorkflow(new List<ExecutionPointer> {tryPointer});
            var tryStep = SetupTryStep(tryStepId);
            
            var innerCatchStepId = 345;
            var outerCatchStepId = 346;
            tryStep.CatchStepsQueue.Enqueue(new KeyValuePair<Type, int>(typeof(ArgumentException), innerCatchStepId));
            tryStep.CatchStepsQueue.Enqueue(new KeyValuePair<Type, int>(typeof(Exception), outerCatchStepId));

            var definition = SetupWorkflowDefinition(tryStep);

            var exception = new ArgumentException("message");
            
            A.CallTo(() => _pointerFactory.BuildCatchPointer(A<WorkflowDefinition>._, A<ExecutionPointer>._,
                    A<ExecutionPointer>._, A<int>._, A<Exception>._))
                .ReturnsLazily(call => new ExecutionPointer{Id = call.Arguments[3].ToString()});
            
            //act
            _subject.Handle(workflow, definition, tryPointer, new WorkflowStepInline(), exception, new Queue<ExecutionPointer>());
            
            //assert
            tryPointer.Active.Should().BeFalse();
            tryPointer.EndTime.Should().Be(_now.ToUniversalTime());
            tryPointer.Status.Should().Be(PointerStatus.Failed);
            
            workflow.ExecutionPointers.Count.Should().Be(2);
            workflow.ExecutionPointers.FindById(innerCatchStepId.ToString()).Should().NotBeNull();
            
            A.CallTo(() => _pointerFactory.BuildCatchPointer(A<WorkflowDefinition>._, A<ExecutionPointer>._,
                    A<ExecutionPointer>._, A<int>._, A<Exception>._))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _pointerFactory.BuildNextPointer(A<WorkflowDefinition>._, A<ExecutionPointer>._,
                A<IStepOutcome>._)).MustNotHaveHappened();
            
            workflow.Status.Should().NotBe(WorkflowStatus.Terminated);
            A.CallTo(() => _eventPublisher.PublishNotification(A<LifeCycleEvent>._))
                .MustNotHaveHappened();
        }
        
        [Fact(DisplayName = "Should catch exception with outer catch step")]
        public void should_catch_exception_with_outer_catch_step()
        {
            //arrange
            var tryStepId = 123;
            var tryPointer = SetupTryPointer(tryStepId);
            var workflow = SetupWorkflow(new List<ExecutionPointer> {tryPointer});
            var tryStep = SetupTryStep(tryStepId);
            
            var innerCatchStepId = 345;
            var outerCatchStepId = 346;
            tryStep.CatchStepsQueue.Enqueue(new KeyValuePair<Type, int>(typeof(ArgumentException), innerCatchStepId));
            tryStep.CatchStepsQueue.Enqueue(new KeyValuePair<Type, int>(typeof(Exception), outerCatchStepId));

            var definition = SetupWorkflowDefinition(tryStep);

            var exception = new Exception("message");
            
            A.CallTo(() => _pointerFactory.BuildCatchPointer(A<WorkflowDefinition>._, A<ExecutionPointer>._,
                    A<ExecutionPointer>._, A<int>._, A<Exception>._))
                .ReturnsLazily(call => new ExecutionPointer{Id = call.Arguments[3].ToString()});
            
            //act
            _subject.Handle(workflow, definition, tryPointer, new WorkflowStepInline(), exception, new Queue<ExecutionPointer>());
            
            //assert
            tryPointer.Active.Should().BeFalse();
            tryPointer.EndTime.Should().Be(_now.ToUniversalTime());
            tryPointer.Status.Should().Be(PointerStatus.Failed);
            
            workflow.ExecutionPointers.Count.Should().Be(2);
            workflow.ExecutionPointers.FindById(outerCatchStepId.ToString()).Should().NotBeNull();
            
            A.CallTo(() => _pointerFactory.BuildCatchPointer(A<WorkflowDefinition>._, A<ExecutionPointer>._,
                    A<ExecutionPointer>._, A<int>._, A<Exception>._))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _pointerFactory.BuildNextPointer(A<WorkflowDefinition>._, A<ExecutionPointer>._,
                A<IStepOutcome>._)).MustNotHaveHappened();
            
            workflow.Status.Should().NotBe(WorkflowStatus.Terminated);
            A.CallTo(() => _eventPublisher.PublishNotification(A<LifeCycleEvent>._))
                .MustNotHaveHappened();
        }

        [Fact(DisplayName = "Should not catch exception because of wrong exception type")]
        public void should_not_catch_exception_because_of_wrong_exception_type()
        {
            //arrange
            var tryStepId = 123;
            var tryPointer = SetupTryPointer(tryStepId);
            var workflow = SetupWorkflow(new List<ExecutionPointer> {tryPointer});
            var tryStep = SetupTryStep(tryStepId);
            
            var catchStepId = 345;
            tryStep.CatchStepsQueue.Enqueue(new KeyValuePair<Type, int>(typeof(ArgumentException), catchStepId));

            var definition = SetupWorkflowDefinition(tryStep);

            var exception = new Exception("message");
            
            A.CallTo(() => _pointerFactory.BuildCatchPointer(A<WorkflowDefinition>._, A<ExecutionPointer>._,
                    A<ExecutionPointer>._, A<int>._, A<Exception>._))
                .Returns(new ExecutionPointer {Id = "catchPointerId"});
            
            //act
            _subject.Handle(workflow, definition, tryPointer, new WorkflowStepInline(), exception, new Queue<ExecutionPointer>());
            
            //assert
            tryPointer.Active.Should().BeFalse();
            tryPointer.EndTime.Should().Be(_now.ToUniversalTime());
            tryPointer.Status.Should().Be(PointerStatus.Failed);
            
            workflow.ExecutionPointers.Count.Should().Be(1);
            
            A.CallTo(() => _pointerFactory.BuildCatchPointer(A<WorkflowDefinition>._, A<ExecutionPointer>._,
                    A<ExecutionPointer>._, A<int>._, A<Exception>._))
                .MustNotHaveHappened();
            A.CallTo(() => _pointerFactory.BuildNextPointer(A<WorkflowDefinition>._, A<ExecutionPointer>._,
                A<IStepOutcome>._)).MustNotHaveHappened();
            
            workflow.Status.Should().Be(WorkflowStatus.Terminated);
            A.CallTo(() => _eventPublisher.PublishNotification(A<LifeCycleEvent>._))
                .MustHaveHappenedOnceExactly();
        }

        private static ExecutionPointer SetupTryPointer(int tryStepId)
        {
            var tryPointerId = "tryPointerId";
            var tryPointer = new ExecutionPointer
            {
                Scope = new List<string>(),
                Id = tryPointerId,
                StepId = tryStepId,
                Active = true,
                Status = PointerStatus.Pending
            };
            return tryPointer;
        }

        private static WorkflowInstance SetupWorkflow(ICollection<ExecutionPointer> pointers)
        {
            return new WorkflowInstance
            {
                ExecutionPointers = new ExecutionPointerCollection(pointers),
                Status = WorkflowStatus.Runnable
            };
        }

        private static WorkflowStep<Sequence> SetupTryStep(int tryStepId)
        {
            return new WorkflowStep<Sequence>
            {
                Id = tryStepId,
                ErrorBehavior = WorkflowErrorHandling.Catch
            };
        }

        private static WorkflowDefinition SetupWorkflowDefinition(WorkflowStep<Sequence> tryStep)
        {
            var definition = new WorkflowDefinition();
            definition.Steps.Add(tryStep);
            return definition;
        }
    }
}