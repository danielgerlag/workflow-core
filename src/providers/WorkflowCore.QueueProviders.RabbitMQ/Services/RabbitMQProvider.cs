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
        private readonly IConnection _connection;
        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };

        public RabbitMQProvider(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _connection = _connectionFactory.CreateConnection("Workflow-Core");
        }

        public async Task<string> DequeueForProcessing()
        {            
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
            using (var channel = _connection.CreateModel())
            {
                channel.QueueDeclare(queue: "wfc.process_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
                var body = Encoding.UTF8.GetBytes(Id);
                channel.BasicPublish(exchange: "", routingKey: "wfc.process_queue", basicProperties: null, body: body);                
            }
        }

        public async Task QueueForPublishing(EventPublication item)
        {            
            using (var channel = _connection.CreateModel())
            {
                channel.QueueDeclare(queue: "wfc.publish_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, SerializerSettings));
                channel.BasicPublish(exchange: "", routingKey: "wfc.publish_queue", basicProperties: null, body: body);
            }
        }

        ~RabbitMQProvider()
        {
            _connection.Close();            
        }
    }    
}
