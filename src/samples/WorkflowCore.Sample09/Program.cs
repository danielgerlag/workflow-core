using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using WorkflowCore.Interface;

namespace WorkflowCore.Sample09
{
    class Program
    {
        public static void Main(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            //start the workflow host
            var host = serviceProvider.GetService<IWorkflowHost>();
            host.RegisterWorkflow<ForEachWorkflow>();
            host.Start();
            
            Console.WriteLine("Starting workflow...");
            string workflowId = host.StartWorkflow("Foreach").Result;

            
            Console.ReadLine();
            host.Stop();
        }

        private static IServiceProvider ConfigureServices()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddWorkflow();
            //services.AddWorkflow(x => x.UseMongoDB(@"mongodb://localhost:27017", "workflow-test002"));
            //services.AddWorkflow(x => x.UseSqlServer(@"Server=.;Database=WorkflowCore3;Trusted_Connection=True;", true, true));            
            //services.AddWorkflow(x => x.UseSqlite(@"Data Source=database2.db;", true));            
            //services.AddWorkflow(x => x.UsePostgreSQL(@"Server=127.0.0.1;Port=32768;Database=workflow_test002;User Id=postgres;", true, true));


            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }

    }
}