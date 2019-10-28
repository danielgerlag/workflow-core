using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WorkflowCore.Interface;
using WorkflowCore.Services;
using WorkflowCore.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using WorkflowCore.Primitives;
using WorkflowCore.Services.BackgroundTasks;
using WorkflowCore.Services.DefinitionStorage;
using WorkflowCore.Services.ErrorHandlers;

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
            services.TryAddSingleton<ISingletonMemoryProvider, MemoryPersistenceProvider>();
            services.TryAddTransient<IPersistenceProvider>(options.PersistanceFactory);
            services.TryAddSingleton<IQueueProvider>(options.QueueFactory);
            services.TryAddSingleton<IDistributedLockProvider>(options.LockFactory);
            services.TryAddSingleton<ILifeCycleEventHub>(options.EventHubFactory);
            services.TryAddSingleton<ISearchIndex>(options.SearchIndexFactory);

            services.TryAddSingleton<IWorkflowRegistry, WorkflowRegistry>();
            services.TryAddSingleton<WorkflowOptions>(options);
            services.TryAddSingleton<ILifeCycleEventPublisher, LifeCycleEventPublisher>();            

            services.TryAddTransient<IBackgroundTask, WorkflowConsumer>();
            services.TryAddTransient<IBackgroundTask, EventConsumer>();
            services.TryAddTransient<IBackgroundTask, IndexConsumer>();
            services.TryAddTransient<IBackgroundTask, RunnablePoller>();
            services.TryAddTransient<IBackgroundTask>(sp => sp.GetService<ILifeCycleEventPublisher>());

            services.TryAddTransient<IWorkflowErrorHandler, CompensateHandler>();
            services.TryAddTransient<IWorkflowErrorHandler, RetryHandler>();
            services.TryAddTransient<IWorkflowErrorHandler, TerminateHandler>();
            services.TryAddTransient<IWorkflowErrorHandler, SuspendHandler>();

            services.TryAddSingleton<IWorkflowController, WorkflowController>();
            services.TryAddSingleton<IWorkflowHost, WorkflowHost>();
            services.TryAddTransient<IScopeProvider, ScopeProvider>();
            services.TryAddTransient<IWorkflowExecutor, WorkflowExecutor>();
            services.TryAddTransient<ICancellationProcessor, CancellationProcessor>();
            services.TryAddTransient<IWorkflowBuilder, WorkflowBuilder>();
            services.TryAddTransient<IDateTimeProvider, DateTimeProvider>();
            services.TryAddTransient<IExecutionResultProcessor, ExecutionResultProcessor>();
            services.TryAddTransient<IExecutionPointerFactory, ExecutionPointerFactory>();

            services.TryAddTransient<IPooledObjectPolicy<IPersistenceProvider>, InjectedObjectPoolPolicy<IPersistenceProvider>>();
            services.TryAddTransient<IPooledObjectPolicy<IWorkflowExecutor>, InjectedObjectPoolPolicy<IWorkflowExecutor>>();

            services.TryAddTransient<ISyncWorkflowRunner, SyncWorkflowRunner>();
            services.TryAddTransient<IDefinitionLoader, DefinitionLoader>();

            services.TryAddTransient<Foreach>();

            return services;
        }
    }
}

