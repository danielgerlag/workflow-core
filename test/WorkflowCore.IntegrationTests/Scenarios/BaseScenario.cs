using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public abstract class BaseScenario<TWorkflow>
        where TWorkflow : IWorkflow, new()
    {
        protected IWorkflowHost Host;
        protected IPersistenceProvider PersistenceProvider;

        public BaseScenario()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            Configure(services);

            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddConsole(LogLevel.Debug);                        

            PersistenceProvider = serviceProvider.GetService<IPersistenceProvider>();
            Host = serviceProvider.GetService<IWorkflowHost>();
            Host.RegisterWorkflow<TWorkflow>();
            Host.Start();
        }

        protected virtual void Configure(IServiceCollection services)
        {
            services.AddWorkflow();
        }
    }
}
