using Machine.Specifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public abstract class BaseScenario<TWorkflow, TData>
        where TWorkflow : IWorkflow<TData>, new()
        where TData : new()
    {
        protected static IWorkflowHost Host;
        protected static IPersistenceProvider PersistenceProvider;
        //protected static string WorkflowId;
        //protected static WorkflowInstance Instance;

        protected Establish context;
        protected Cleanup after;

        public BaseScenario()
        {
            context = EstablishContext;
            after = CleanupAfter;
        }

        protected abstract void ConfigureWorkflow(IServiceCollection services);

        protected virtual void EstablishContext()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            ConfigureWorkflow(services);
            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddConsole(LogLevel.Debug);            
                        
            PersistenceProvider = serviceProvider.GetService<IPersistenceProvider>();
            Host = serviceProvider.GetService<IWorkflowHost>();
            Host.RegisterWorkflow<TWorkflow, TData>();
            Host.Start();
        }

        protected virtual void CleanupAfter()
        {
            Host.Stop();            
            Host = null;
            //WorkflowId = null;
            //Instance = null;
            PersistenceProvider = null;
        }
    }
}
