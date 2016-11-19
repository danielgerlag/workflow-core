using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Models;
using WorkflowCore.QueueProviders.RabbitMQ.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseRabbitMQ(this WorkflowOptions options, IConnectionFactory connectionFactory)
        {
            options.UseQueueProvider(sp => new RabbitMQProvider(connectionFactory));
            return options;
        }
    }
}
