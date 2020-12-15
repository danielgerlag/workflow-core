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
            services.AddSingleton<ISingletonMemoryProvider, MemoryPersistenceProvider>();
            services.AddTransient<IPersistenceProvider>(options.PersistanceFactory);
            services.AddTransient<IWorkflowRepository>(options.PersistanceFactory);
            services.AddTransient<ISubscriptionRepository>(options.PersistanceFactory);
            services.AddTransient<IEventRepository>(options.PersistanceFactory);
            services.AddSingleton<IQueueProvider>(options.QueueFactory);
            services.AddSingleton<IDistributedLockProvider>(options.LockFactory);
            services.AddSingleton<ILifeCycleEventHub>(options.EventHubFactory);
            services.AddSingleton<ISearchIndex>(options.SearchIndexFactory);

            services.AddSingleton<IWorkflowRegistry, WorkflowRegistry>();
            services.AddSingleton<WorkflowOptions>(options);
            services.AddSingleton<ILifeCycleEventPublisher, LifeCycleEventPublisher>();

            if (options.EnableWorkflows)
            {
                services.AddTransient<IBackgroundTask, WorkflowConsumer>();
            }

            if (options.EnableEvents)
            {
                services.AddTransient<IBackgroundTask, EventConsumer>();
            }

            if (options.EnableIndexes)
            {
                services.AddTransient<IBackgroundTask, IndexConsumer>();
            }

            if (options.EnablePolling)
            {
                services.AddTransient<IBackgroundTask, RunnablePoller>();
            }

            services.AddTransient<IBackgroundTask>(sp => sp.GetService<ILifeCycleEventPublisher>());

            services.AddTransient<IWorkflowErrorHandler, CompensateHandler>();
            services.AddTransient<IWorkflowErrorHandler, RetryHandler>();
            services.AddTransient<IWorkflowErrorHandler, TerminateHandler>();
            services.AddTransient<IWorkflowErrorHandler, SuspendHandler>();

            services.AddSingleton<IGreyList, GreyList>();
            services.AddSingleton<IWorkflowController, WorkflowController>();
            services.AddSingleton<IActivityController, ActivityController>();
            services.AddSingleton<IWorkflowHost, WorkflowHost>();
            services.AddTransient<IStepExecutor, StepExecutor>();
            services.AddTransient<IWorkflowMiddlewareErrorHandler, DefaultWorkflowMiddlewareErrorHandler>();
            services.AddTransient<IWorkflowMiddlewareRunner, WorkflowMiddlewareRunner>();
            services.AddTransient<IScopeProvider, ScopeProvider>();
            services.AddTransient<IWorkflowExecutor, WorkflowExecutor>();
            services.AddTransient<ICancellationProcessor, CancellationProcessor>();
            services.AddTransient<IWorkflowBuilder, WorkflowBuilder>();
            services.AddTransient<IDateTimeProvider, DateTimeProvider>();
            services.AddTransient<IExecutionResultProcessor, ExecutionResultProcessor>();
            services.AddTransient<IExecutionPointerFactory, ExecutionPointerFactory>();

            services.AddTransient<IPooledObjectPolicy<IPersistenceProvider>, InjectedObjectPoolPolicy<IPersistenceProvider>>();
            services.AddTransient<IPooledObjectPolicy<IWorkflowExecutor>, InjectedObjectPoolPolicy<IWorkflowExecutor>>();

            services.AddTransient<ISyncWorkflowRunner, SyncWorkflowRunner>();

            services.AddTransient<Foreach>();

            return services;
        }

        /// <summary>
        /// Adds a middleware that will run around the execution of a workflow step.
        /// </summary>
        /// <param name="services">The services collection.</param>
        /// <param name="factory">Optionally configure using your own factory.</param>
        /// <typeparam name="TMiddleware">The type of middleware.
        /// It must implement <see cref="IWorkflowStepMiddleware"/>.</typeparam>
        /// <returns>The services collection for chaining.</returns>
        public static IServiceCollection AddWorkflowStepMiddleware<TMiddleware>(
            this IServiceCollection services,
            Func<IServiceProvider, TMiddleware> factory = null)
            where TMiddleware : class, IWorkflowStepMiddleware =>
                factory == null
                    ? services.AddTransient<IWorkflowStepMiddleware, TMiddleware>()
                    : services.AddTransient<IWorkflowStepMiddleware, TMiddleware>(factory);

        /// <summary>
        /// Adds a middleware that will run either before a workflow is kicked off or after
        /// a workflow completes. Specify the phase of the workflow execution process that
        /// you want to execute this middleware using <see cref="IWorkflowMiddleware.Phase"/>.
        /// </summary>
        /// <param name="services">The services collection.</param>
        /// <param name="factory">Optionally configure using your own factory.</param>
        /// <typeparam name="TMiddleware">The type of middleware.
        /// It must implement <see cref="IWorkflowMiddleware"/>.</typeparam>
        /// <returns>The services collection for chaining.</returns>
        public static IServiceCollection AddWorkflowMiddleware<TMiddleware>(
            this IServiceCollection services,
            Func<IServiceProvider, TMiddleware> factory = null)
            where TMiddleware : class, IWorkflowMiddleware =>
                factory == null
                    ? services.AddTransient<IWorkflowMiddleware, TMiddleware>()
                    : services.AddTransient<IWorkflowMiddleware, TMiddleware>(factory);
    }
}

