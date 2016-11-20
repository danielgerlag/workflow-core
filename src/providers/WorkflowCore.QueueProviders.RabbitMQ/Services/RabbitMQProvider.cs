using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.QueueProviders.RabbitMQ.Services
{
    public class RabbitMQProvider : IQueueProvider
    {
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _connection = null;
        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };

        public RabbitMQProvider(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<string> DequeueForProcessing()
        {
            if (_connection == null)
                throw new Exception("RabbitMQ provider not running");

            using (var channel = _connection.CreateModel())
            {
                channel.QueueDeclare(queue: "wfc.process_queue",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                
                var msg = channel.BasicGet("wfc.process_queue", false);
                if (msg != null)
                {                    
                    var data = Encoding.UTF8.GetString(msg.Body);
                    channel.BasicAck(msg.DeliveryTag, false);
                    return data;
                }
                return null;
            }
        }

        public async Task<EventPublication> DequeueForPublishing()
        {
            if (_connection == null)
                throw new Exception("RabbitMQ provider not running");

            using (var channel = _connection.CreateModel())
            {
                channel.QueueDeclare(queue: "wfc.publish_queue",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                var msg = channel.BasicGet("wfc.publish_queue", false);
                if (msg != null)
                {                    
                    var dataRaw = Encoding.UTF8.GetString(msg.Body);
                    var result = JsonConvert.DeserializeObject<EventPublication>(dataRaw, SerializerSettings);
                    channel.BasicAck(msg.DeliveryTag, false);
                    return result;
                }
                return null;
            }
        }

        public async Task QueueForProcessing(string Id)
        {
            if (_connection == null)
                throw new Exception("RabbitMQ provider not running");

            using (var channel = _connection.CreateModel())
            {
                channel.QueueDeclare(queue: "wfc.process_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
                var body = Encoding.UTF8.GetBytes(Id);
                channel.BasicPublish(exchange: "", routingKey: "wfc.process_queue", basicProperties: null, body: body);                
            }
        }

        public async Task QueueForPublishing(EventPublication item)
        {
            if (_connection == null)
                throw new Exception("RabbitMQ provider not running");

            using (var channel = _connection.CreateModel())
            {
                channel.QueueDeclare(queue: "wfc.publish_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, SerializerSettings));
                channel.BasicPublish(exchange: "", routingKey: "wfc.publish_queue", basicProperties: null, body: body);
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

        public void Start()
        {
            _connection = _connectionFactory.CreateConnection("Workflow-Core");
        }

        public void Stop()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection = null;
            }
        }
    }    
}
