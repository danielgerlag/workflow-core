using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Services;

namespace WorkflowCore.Sample01
{
    public class Program
    {
        
        public static void Main(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            //start the workflow runtime
            var runtime = serviceProvider.GetService<IWorkflowRuntime>();
            var registry = serviceProvider.GetService<IWorkflowRegistry>();
            registry.RegisterWorkflow(new HelloWorldWorkflow());
            runtime.StartRuntime();

            runtime.StartWorkflow("HelloWorld", 1, null);
            
            Console.ReadLine();
            runtime.StopRuntime();
        }

        private static IServiceProvider ConfigureServices()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddWorkflow(x => x.UsePersistence(sp => new MemoryPersistenceProvider()));
            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddConsole(LogLevel.Debug, true);
            loggerFactory.AddDebug();
            return serviceProvider;
        }


    }
}
