using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using WorkflowCore.Interface;
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
            eventHubMock
                .Setup(hub => hub.PublishNotification(It.IsAny<StepCompleted>()))
                .Callback(() => wasCalled.SetResult(true));
            LifeCycleEventPublisher publisher = new LifeCycleEventPublisher(eventHubMock.Object, new LoggerFactory());

            // Act
            publisher.Start();
            publisher.PublishNotification(new StepCompleted());

            // Assert
            await wasCalled.Task;
        }

        [Fact(DisplayName = "Notifications should be published when the publisher is running")]
        public async Task PublishNotification_WhenRestarted_PublishesNotification()
        {
            // Arrange
            var wasCalled = new TaskCompletionSource<bool>();
            var eventHubMock = new Mock<ILifeCycleEventHub>();
            eventHubMock
                .Setup(hub => hub.PublishNotification(It.IsAny<StepCompleted>()))
                .Callback(() => wasCalled.SetResult(true));
            LifeCycleEventPublisher publisher = new LifeCycleEventPublisher(eventHubMock.Object, new LoggerFactory());

            // Act
            publisher.Start();
            publisher.Stop();
            publisher.Start();
            publisher.PublishNotification(new StepCompleted());

            // Assert
            await wasCalled.Task;
        }
    }
}