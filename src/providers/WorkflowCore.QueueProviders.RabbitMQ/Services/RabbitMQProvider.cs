using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.QueueProviders.RabbitMQ.Interfaces;

namespace WorkflowCore.QueueProviders.RabbitMQ.Services
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public class RabbitMQProvider : IQueueProvider
    {
        private readonly IRabbitMqQueueNameProvider _queueNameProvider;
        private readonly RabbitMqConnectionFactory _rabbitMqConnectionFactory;
        private readonly IServiceProvider _serviceProvider;
        
        private IConnection _connection = null;
        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        public bool IsDequeueBlocking => false;

        public RabbitMQProvider(IServiceProvider serviceProvider,
            IRabbitMqQueueNameProvider queueNameProvider,
            RabbitMqConnectionFactory connectionFactory)
        {
            _serviceProvider = serviceProvider;
            _queueNameProvider = queueNameProvider;
            _rabbitMqConnectionFactory = connectionFactory;
        }

        public async Task QueueWork(string id, QueueType queue)
        {
            if (_connection == null)
                throw new InvalidOperationException("RabbitMQ provider not running");

            var channel = await _connection.CreateChannelAsync(new CreateChannelOptions(publisherConfirmationsEnabled: false, publisherConfirmationTrackingEnabled: false), CancellationToken.None);
            try
            {
                await channel.QueueDeclareAsync(queue: _queueNameProvider.GetQueueName(queue), durable: true, exclusive: false, autoDelete: false, arguments: null, passive: false, noWait: false, CancellationToken.None);
                var body = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(id));
                await channel.BasicPublishAsync(exchange: "", routingKey: _queueNameProvider.GetQueueName(queue), mandatory: false, basicProperties: new BasicProperties(), body: body, CancellationToken.None);
            }
            finally
            {
                await channel.CloseAsync(200, "OK", abort: false, CancellationToken.None);
            }
        }

        public async Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            if (_connection == null)
                throw new InvalidOperationException("RabbitMQ provider not running");

            var channel = await _connection.CreateChannelAsync(new CreateChannelOptions(publisherConfirmationsEnabled: false, publisherConfirmationTrackingEnabled: false), CancellationToken.None);
            try
            {
                await channel.QueueDeclareAsync(queue: _queueNameProvider.GetQueueName(queue),
                                         durable: true,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null,
                                         passive: false,
                                         noWait: false,
                                         CancellationToken.None);

                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, CancellationToken.None);

                var msg = await channel.BasicGetAsync(_queueNameProvider.GetQueueName(queue), autoAck: false, CancellationToken.None);
                if (msg != null)
                {
                    var data = Encoding.UTF8.GetString(msg.Body.ToArray());
                    await channel.BasicAckAsync(msg.DeliveryTag, multiple: false, CancellationToken.None);
                    return data;
                }
                return null;
            }
            finally
            {
                await channel.CloseAsync(200, "OK", abort: false, CancellationToken.None);
            }
        }
        
        public void Dispose()
        {
            if (_connection != null)
            {
                if (_connection.IsOpen)
                    _connection.CloseAsync(200, "OK", TimeSpan.FromSeconds(10), abort: false, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        public async Task Start()
        {
            _connection = await _rabbitMqConnectionFactory(_serviceProvider, "Workflow-Core");
        }

        public async Task Stop()
        {
            if (_connection != null)
            {
                await _connection.CloseAsync(200, "OK", TimeSpan.FromSeconds(10), abort: false, CancellationToken.None);
                _connection = null;
            }
        }

    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
