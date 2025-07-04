using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            return options.UseRabbitMQ(async (sp, name) => await connectionFactory.CreateConnectionAsync(name));
        }
        
        public static WorkflowOptions UseRabbitMQ(this WorkflowOptions options,
            IConnectionFactory connectionFactory,
            IEnumerable<string> hostnames)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));
            if (hostnames == null) throw new ArgumentNullException(nameof(hostnames));

            return options.UseRabbitMQ(async (sp, name) => await connectionFactory.CreateConnectionAsync(hostnames.ToList(), name));
        }

        private static WorkflowOptions UseRabbitMQ(this WorkflowOptions options, Func<IServiceProvider, string, Task<IConnection>> rabbitMqConnectionFactory)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (rabbitMqConnectionFactory == null) throw new ArgumentNullException(nameof(rabbitMqConnectionFactory));

            options.Services.AddSingleton(rabbitMqConnectionFactory);
        
            options.Services.AddSingleton<RabbitMqConnectionFactory>(
                sp => (provider, name) =>
                {
                    var connection = rabbitMqConnectionFactory(provider, name).GetAwaiter().GetResult();
                    return connection;
                });
            
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
