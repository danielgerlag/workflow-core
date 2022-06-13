using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;
using WorkflowCore.Services;
using Xunit;

namespace WorkflowCore.UnitTests.Services
{
    public class LifeCycleEventPublisherTests
    {
        [Fact(DisplayName = "Notifications should be published when the publisher is running")]
        public async Task PublishNotification_WhenStarted_PublishesNotification()
        {
            // Arrange
            var wasCalled = new TaskCompletionSource<bool>();
            var eventHubMock = new Mock<ILifeCycleEventHub>();
            var serviceCollectionMock = new Mock<IServiceCollection>();

            var workflowOptions = new WorkflowOptions(serviceCollectionMock.Object)
            {
                EnableLifeCycleEventsPublisher = true
            };

            eventHubMock
                .Setup(hub => hub.PublishNotification(It.IsAny<StepCompleted>()))
                .Callback(() => wasCalled.SetResult(true));
            LifeCycleEventPublisher publisher = new LifeCycleEventPublisher(eventHubMock.Object, workflowOptions, new LoggerFactory());

            // Act
            publisher.Start();
            publisher.PublishNotification(new StepCompleted());

            // Assert
            await wasCalled.Task;
            eventHubMock.Verify(hub => hub.PublishNotification(It.IsAny<StepCompleted>()), Times.Once());
        }

        [Fact(DisplayName = "Notifications should be published when the publisher is running")]
        public async Task PublishNotification_WhenRestarted_PublishesNotification()
        {
            // Arrange
            var wasCalled = new TaskCompletionSource<bool>();
            var eventHubMock = new Mock<ILifeCycleEventHub>();
            var serviceCollectionMock = new Mock<IServiceCollection>();

            var workflowOptions = new WorkflowOptions(serviceCollectionMock.Object)
            {
                EnableLifeCycleEventsPublisher = true
            };

            eventHubMock
                .Setup(hub => hub.PublishNotification(It.IsAny<StepCompleted>()))
                .Callback(() => wasCalled.SetResult(true));
            LifeCycleEventPublisher publisher = new LifeCycleEventPublisher(eventHubMock.Object, workflowOptions, new LoggerFactory());

            // Act
            publisher.Start();
            publisher.Stop();
            publisher.Start();
            publisher.PublishNotification(new StepCompleted());

            // Assert
            await wasCalled.Task;
            eventHubMock.Verify(hub => hub.PublishNotification(It.IsAny<StepCompleted>()), Times.Once());
        }

        [Fact(DisplayName = "Notifications should be disabled if option EnableLifeCycleEventsPublisher is disabled")]
        public void PublishNotification_Disabled()
        {
            // Arrange
            var eventHubMock = new Mock<ILifeCycleEventHub>();
            var serviceCollectionMock = new Mock<IServiceCollection>();

            var workflowOptions = new WorkflowOptions(serviceCollectionMock.Object)
            {
                EnableLifeCycleEventsPublisher = false
            };

            eventHubMock
                .Setup(hub => hub.PublishNotification(It.IsAny<StepCompleted>()))
                .Returns(Task.CompletedTask);
            LifeCycleEventPublisher publisher = new LifeCycleEventPublisher(eventHubMock.Object, workflowOptions, new LoggerFactory());

            // Act
            publisher.Start();
            publisher.PublishNotification(new StepCompleted());
            publisher.Stop();

            // Assert
            eventHubMock.Verify(hub => hub.PublishNotification(It.IsAny<StepCompleted>()), Times.Never());
        }
    }
}