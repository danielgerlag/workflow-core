using RabbitMQ.Client;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.QueueProviders.RabbitMQ.Interfaces;
using System.Collections.Generic;

namespace WorkflowCore.QueueProviders.RabbitMQ.Services
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public class RabbitMQProvider : IQueueProvider
    {
        private readonly IRabbitMqQueueNameProvider _queueNameProvider;
        private readonly RabbitMqConnectionFactory _rabbitMqConnectionFactory;
        private readonly IServiceProvider _serviceProvider;
        
        private IConnection _connection = null;

        public bool IsDequeueBlocking => false;

        public RabbitMQProvider(IServiceProvider serviceProvider,
            IRabbitMqQueueNameProvider queueNameProvider,
            RabbitMqConnectionFactory connectionFactory)
        {
            _serviceProvider = serviceProvider;
            _queueNameProvider = queueNameProvider;
            _rabbitMqConnectionFactory = connectionFactory;
        }

        private IEnumerable<string> GetQueuesSortedByPriority(QueueType queue)
        {
            return _queueNameProvider.GetAll()
                .Where(kvp => kvp.Key.Item1 == queue)
                .OrderByDescending(kvp => kvp.Key.Item2)
                .Select(kvp => kvp.Value);
        }

        public async Task QueueWork(string id, QueueType queue, QueuePriority priority)
        {
            if (_connection == null)
                throw new InvalidOperationException("RabbitMQ provider not running");

            using (var channel = _connection.CreateModel())
            {
                channel.QueueDeclare(queue: _queueNameProvider.GetQueueName(queue), durable: true, exclusive: false, autoDelete: false, arguments: null);
                var body = Encoding.UTF8.GetBytes(id);
                channel.BasicPublish(exchange: "", routingKey: _queueNameProvider.GetQueueName(queue), basicProperties: null, body: body);
            }
        }

        public Task QueueWork(string id, QueueType queue)
        {
            return QueueWork(id, queue, QueuePriority.Normal);
        }

        private async Task<string> DequeueWork(string queueName)
        {
            if (_connection == null)
                throw new InvalidOperationException("RabbitMQ provider not running");

            using (var channel = _connection.CreateModel())
            {
                channel.QueueDeclare(queue: queueName,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                var msg = channel.BasicGet(queueName, false);
                if (msg != null)
                {
                    var data = Encoding.UTF8.GetString(msg.Body);
                    channel.BasicAck(msg.DeliveryTag, false);
                    return data;
                }
                return null;
            }
        }

        public async Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            foreach (var q in GetQueuesSortedByPriority(queue))
            {
                var result = await DequeueWork(q);
                if (result != null)
                    return result;
            }

            return null;
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                if (_connection.IsOpen)
                    _connection.Close();
            }
        }

        public async Task Start()
        {
            _connection = _rabbitMqConnectionFactory(_serviceProvider, "Workflow-Core");
        }

        public async Task Stop()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection = null;
            }
        }

    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
