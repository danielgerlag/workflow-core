using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.QueueProviders.RabbitMQ.Interfaces;
using WorkflowCore.QueueProviders.RabbitMQ.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public delegate IConnection RabbitMqConnectionFactory(IServiceProvider sp, string clientProvidedName);
    
    public static class ServiceCollectionExtensions
    {
        public static WorkflowOptions UseRabbitMQ(this WorkflowOptions options, IConnectionFactory connectionFactory)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));

            return options
                .UseRabbitMQ((sp, name) => connectionFactory.CreateConnection(name));
        }
        
        public static WorkflowOptions UseRabbitMQ(this WorkflowOptions options,
            IConnectionFactory connectionFactory,
            IEnumerable<string> hostnames)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));
            if (hostnames == null) throw new ArgumentNullException(nameof(hostnames));

            return options
                .UseRabbitMQ((sp, name) => connectionFactory.CreateConnection(hostnames.ToList(), name));
        }
        
        public static WorkflowOptions UseRabbitMQ(this WorkflowOptions options, RabbitMqConnectionFactory rabbitMqConnectionFactory)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (rabbitMqConnectionFactory == null) throw new ArgumentNullException(nameof(rabbitMqConnectionFactory));

            options.Services.AddSingleton(rabbitMqConnectionFactory);
            options.Services.TryAddSingleton<IRabbitMqQueueNameProvider, DefaultRabbitMqQueueNameProvider>();
            options.UseQueueProvider(RabbitMqQueueProviderFactory);
            
            return options;
        }

        private static IQueueProvider RabbitMqQueueProviderFactory(IServiceProvider sp)
            => new RabbitMQProvider(sp,
                sp.GetRequiredService<IRabbitMqQueueNameProvider>(),
                sp.GetRequiredService<RabbitMqConnectionFactory>());
    }
}
