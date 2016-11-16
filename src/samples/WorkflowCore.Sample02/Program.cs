using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Services;


namespace WorkflowCore.Sample02
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            //start the workflow runtime
            var runtime = serviceProvider.GetService<IWorkflowRuntime>();
            runtime.RegisterWorkflow<SimpleDecisionWorkflow>();
            runtime.StartRuntime();

            runtime.StartWorkflow("Simple Decision Workflow", 1, null);

            Console.ReadLine();
            runtime.StopRuntime();
        }

        private static IServiceProvider ConfigureServices()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddWorkflow(wf =>
            {
                wf.UsePersistence(sp => new MemoryPersistenceProvider());
                wf.UseThreads(1);
            });
            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddDebug();
            return serviceProvider;
        }
    }
}
