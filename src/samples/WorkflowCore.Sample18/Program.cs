using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using WorkflowCore.Interface;

namespace WorkflowCore.Sample18
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            //start the workflow host
            var host = serviceProvider.GetService<IWorkflowHost>();
            host.RegisterWorkflow<WaitWorkflow>();
            host.Start();

            Console.WriteLine("Start Workflow)");

            string workflowId = await host.StartWorkflow(nameof(WaitWorkflow));

            Console.WriteLine("Wait for Workflow");

            await host.WaitForWorkflow(workflowId);

            Console.WriteLine("End of Workflow");

            Console.ReadLine();
            host.Stop();
        }

        private static IServiceProvider ConfigureServices()
        {
            //setup dependency injection
            return new ServiceCollection()
                .AddLogging()
                .AddWorkflow()
                .BuildServiceProvider();
        }
    }
}
