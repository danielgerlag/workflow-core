using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.QueueProviders.RabbitMQ.Services
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public class RabbitMQProvider : IQueueProvider
    {
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _connection = null;
        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };

        public bool IsDequeueBlocking => false;

        public RabbitMQProvider(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task QueueWork(string id, QueueType queue)
        {
            if (_connection == null)
                throw new InvalidOperationException("RabbitMQ provider not running");

            using (var channel = _connection.CreateModel())
            {
                channel.QueueDeclare(queue: GetQueueName(queue), durable: true, exclusive: false, autoDelete: false, arguments: null);
                var body = Encoding.UTF8.GetBytes(id);
                channel.BasicPublish(exchange: "", routingKey: GetQueueName(queue), basicProperties: null, body: body);
            }
        }

        public async Task<string> DequeueWork(QueueType queue, CancellationToken cancellationToken)
        {
            if (_connection == null)
                throw new InvalidOperationException("RabbitMQ provider not running");

            using (var channel = _connection.CreateModel())
            {
                channel.QueueDeclare(queue: GetQueueName(queue),
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                var msg = channel.BasicGet(GetQueueName(queue), false);
                if (msg != null)
                {
                    var data = Encoding.UTF8.GetString(msg.Body);
                    channel.BasicAck(msg.DeliveryTag, false);
                    return data;
                }
                return null;
            }
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
            _connection = _connectionFactory.CreateConnection("Workflow-Core");
        }

        public async Task Stop()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection = null;
            }
        }

        private string GetQueueName(QueueType queue)
        {
            switch (queue)
            {
                case QueueType.Workflow:
                    return "wfc.workflow_queue";
                case QueueType.Event:
                    return "wfc.event_queue";
                case QueueType.Index:
                    return "wfc.index_queue";
            }
            return null;
        }
                
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
