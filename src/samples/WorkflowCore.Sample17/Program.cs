using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;

namespace WorkflowCore.Sample17
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = ConfigureServices();

            //start the workflow host
            var host = serviceProvider.GetService<IWorkflowHost>();
            host.RegisterWorkflow<CompensatingWorkflow>();
            host.Start();

            Console.WriteLine("Starting workflow...");
            var workflowId = host.StartWorkflow("compensate-sample").Result;

            Console.ReadLine();
            host.Stop();
        }

        private static IServiceProvider ConfigureServices()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddWorkflow();

            var serviceProvider = services.BuildServiceProvider();                        
            return serviceProvider;
        }
    }
}
