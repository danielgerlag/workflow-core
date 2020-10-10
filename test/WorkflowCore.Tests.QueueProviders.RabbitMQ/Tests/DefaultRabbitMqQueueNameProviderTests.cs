using FluentAssertions;
using WorkflowCore.Interface;
using WorkflowCore.QueueProviders.RabbitMQ.Services;
using Xunit;

namespace WorkflowCore.Tests.QueueProviders.RabbitMQ.Tests
{
    public class DefaultRabbitMqQueueNameProviderTests
    {
        private readonly DefaultRabbitMqQueueNameProvider _sut;

        public DefaultRabbitMqQueueNameProviderTests()
        {
            _sut = new DefaultRabbitMqQueueNameProvider();
        }

        [Theory]
        [InlineData(QueueType.Event, "wfc.event_queue")]
        [InlineData(QueueType.Index, "wfc.index_queue")]
        [InlineData(QueueType.Workflow, "wfc.workflow_queue")]
        public void GetQueueName_ValidInput_ReturnsValidQueueName(QueueType queueType, string queueName)
        {
            var result = _sut.GetQueueName(queueType);

            result.Should().Be(queueName);
        }
    }
}