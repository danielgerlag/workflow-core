using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Services;
using WorkflowCore.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using WorkflowCore.Primitives;
using WorkflowCore.Services.BackgroundTasks;
using WorkflowCore.Services.DefinitionStorage;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWorkflow(this IServiceCollection services, Action<WorkflowOptions> setupAction = null)
        {
            if (services.Any(x => x.ServiceType == typeof(WorkflowOptions)))
                throw new InvalidOperationException("Workflow services already registered");

            var options = new WorkflowOptions(services);
            setupAction?.Invoke(options);
            services.AddTransient<IPersistenceProvider>(options.PersistanceFactory);
            services.AddSingleton<IQueueProvider>(options.QueueFactory);
            services.AddSingleton<IDistributedLockProvider>(options.LockFactory);
            services.AddSingleton<IWorkflowRegistry, WorkflowRegistry>();
            services.AddSingleton<WorkflowOptions>(options);

            services.AddTransient<IBackgroundTask, WorkflowConsumer>();
            services.AddTransient<IBackgroundTask, EventConsumer>();
            services.AddTransient<IBackgroundTask, RunnablePoller>();

            services.AddSingleton<IWorkflowController, WorkflowController>();
            services.AddSingleton<IWorkflowHost, WorkflowHost>();
            services.AddTransient<IWorkflowExecutor, WorkflowExecutor>();
            services.AddTransient<IWorkflowBuilder, WorkflowBuilder>();
            services.AddTransient<IDateTimeProvider, DateTimeProvider>();
            services.AddTransient<IExecutionResultProcessor, ExecutionResultProcessor>();
            services.AddTransient<IExecutionPointerFactory, ExecutionPointerFactory>();

            services.AddTransient<IPooledObjectPolicy<IPersistenceProvider>, InjectedObjectPoolPolicy<IPersistenceProvider>>();
            services.AddTransient<IPooledObjectPolicy<IWorkflowExecutor>, InjectedObjectPoolPolicy<IWorkflowExecutor>>();

            services.AddTransient<IDefinitionLoader, DefinitionLoader>();

            services.AddTransient<Foreach>();

            return services;
        }
    }
}

