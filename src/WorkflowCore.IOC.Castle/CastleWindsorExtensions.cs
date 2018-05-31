using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.MsDependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Serilog;
using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Primitives;
using WorkflowCore.QueueProviders.SqlServer;
using WorkflowCore.QueueProviders.SqlServer.Interfaces;
using WorkflowCore.QueueProviders.SqlServer.Services;
using WorkflowCore.Services;
using WorkflowCore.Services.BackgroundTasks;
using WorkflowCore.Services.DefinitionStorage;

namespace WorkflowCore.IOC.Castle
{
    public static class CastleWindsorExtensions
    {
        public static void AddWorkflow(this IWindsorContainer container, Action<WorkflowOptions> setupAction = null)
        {
            var services = new ServiceCollection();
            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
            var options = new WorkflowOptions(services);
            var provider = WindsorRegistrationHelper.CreateServiceProvider(container, services);
            services.AddSingleton(container);
            services.AddSingleton(provider);
            setupAction?.Invoke(options);

            container.Register(Component.For<IServiceProvider>().UsingFactoryMethod(p => provider).LifestyleSingleton());

            container.Register(Component.For<IPersistenceProvider>().UsingFactoryMethod(p => options.PersistanceFactory.Invoke(provider)).LifestyleTransient());
            container.Register(Component.For<IQueueProvider>().UsingFactoryMethod(p => options.QueueFactory.Invoke(provider)).LifestyleSingleton());
            container.Register(Component.For<IDistributedLockProvider>().UsingFactoryMethod(p => options.LockFactory.Invoke(provider)).LifestyleSingleton());

            container.Register(Component.For<IWorkflowRegistry>().ImplementedBy< WorkflowRegistry>().LifestyleSingleton());
            container.Register(Component.For<WorkflowOptions>().UsingFactoryMethod(p => options).LifestyleSingleton());

            container.Register(Component.For<IBackgroundTask>().ImplementedBy<WorkflowConsumer>().LifestyleTransient());
            container.Register(Component.For<IBackgroundTask>().ImplementedBy<EventConsumer>().LifestyleTransient());
            container.Register(Component.For<IBackgroundTask>().ImplementedBy<RunnablePoller>().LifestyleTransient());
            container.Register(Component.For<IWorkflowController>().ImplementedBy<WorkflowController>().LifestyleTransient());
            container.Register(Component.For<IWorkflowHost>().ImplementedBy<WorkflowHost>().LifestyleTransient());
            container.Register(Component.For<IWorkflowExecutor>().ImplementedBy<WorkflowExecutor>().LifestyleTransient());
            container.Register(Component.For<IWorkflowBuilder>().ImplementedBy<WorkflowBuilder>().LifestyleTransient());
            container.Register(Component.For<IDateTimeProvider>().ImplementedBy<DateTimeProvider>().LifestyleTransient());
            container.Register(Component.For<IExecutionResultProcessor>().ImplementedBy<ExecutionResultProcessor>().LifestyleTransient());
            container.Register(Component.For<IExecutionPointerFactory>().ImplementedBy<ExecutionPointerFactory>().LifestyleTransient());

            container.Register(Component.For<IPooledObjectPolicy<IPersistenceProvider>>().ImplementedBy<InjectedObjectPoolPolicy<IPersistenceProvider>>().LifestyleTransient());
            container.Register(Component.For<IPooledObjectPolicy<IWorkflowExecutor>>().ImplementedBy<InjectedObjectPoolPolicy<IWorkflowExecutor>>().LifestyleTransient());
            container.Register(Component.For<IDefinitionLoader>().ImplementedBy<DefinitionLoader>().LifestyleTransient());
            //container.Register(Component.For<Foreach>().ImplementedBy<Foreach>().LifestyleTransient());
        }

        /// <summary>
        ///     Use SQL Server as a queue provider
        /// </summary>
        /// <param name="options"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static WorkflowOptions UseCastleWindsorSqlServerBroker(this WorkflowOptions options, string connectionString, bool canCreateDb, bool canMigrateDb)
        {
            var services = options.Services;
            var provider = services.BuildServiceProvider();
            var container = provider.GetService<IWindsorContainer>();
            container.Register(Component.For<IQueueConfigProvider>().ImplementedBy<QueueConfigProvider>().LifestyleTransient());
            container.Register(Component.For<ISqlCommandExecutor>().ImplementedBy<SqlCommandExecutor>().LifestyleTransient());
            container.Register(Component.For<ISqlServerQueueProviderMigrator>().UsingFactoryMethod(p => new SqlServerQueueProviderMigrator(connectionString, p.Resolve<IQueueConfigProvider>(), p.Resolve<ISqlCommandExecutor>())).LifestyleSingleton());

            var sqlOptions = new SqlServerQueueProviderOptions()
            {
                ConnectionString = connectionString,
                CanCreateDb = canCreateDb,
                CanMigrateDb = canMigrateDb
            };

            options.UseQueueProvider(sp =>
            {
                return new SqlServerQueueProvider(sqlOptions, sp.GetService<IQueueConfigProvider>(), sp.GetService<ISqlServerQueueProviderMigrator>(), sp.GetService<ISqlCommandExecutor>());
            });

            return options;
        }
    }
}
