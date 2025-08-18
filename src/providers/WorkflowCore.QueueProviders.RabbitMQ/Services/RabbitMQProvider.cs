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

        private IConnection _connection;

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

            using (var channel = await _connection.CreateChannelAsync())
            {
                await channel.QueueDeclareAsync(queue: _queueNameProvider.GetQueueName(queue), durable: true, exclusive: false,
                    autoDelete: false, arguments: null);
                var body = Encoding.UTF8.GetBytes(id);
                
                await channel.BasicPublishAsync("", _queueNameProvider.GetQueueName(queue), false,body);
            }
        }

        public async Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            if (_connection == null)
                throw new InvalidOperationException("RabbitMQ provider not running");

            using (var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken))
            {
                await channel.QueueDeclareAsync(queue: _queueNameProvider.GetQueueName(queue),
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null, cancellationToken: cancellationToken);

                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false,
                    cancellationToken: cancellationToken);

                var msg = await channel.BasicGetAsync(_queueNameProvider.GetQueueName(queue), false, cancellationToken);

                if (msg == null)
                {
                    return null;
                }
                
                var data = Encoding.UTF8.GetString(msg.Body.ToArray());
                await channel.BasicAckAsync(msg.DeliveryTag, false, cancellationToken);
                return data;
            }
        }

        public void Dispose()
        {
            if (_connection == null) return;
            if (_connection.IsOpen)
                _connection.CloseAsync();
        }

        public async Task Start()
        {
            _connection = _rabbitMqConnectionFactory(_serviceProvider, "Workflow-Core");
        }

        public async Task Stop()
        {
            if (_connection != null)
            {
                await _connection.CloseAsync();
                _connection = null;
            }
        }
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}