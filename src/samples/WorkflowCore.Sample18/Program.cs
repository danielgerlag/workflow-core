using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using System;
using WorkflowCore.Interface;

namespace WorkflowCore.Sample18
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = ConfigureServices();

            //start the workflow host
            var host = serviceProvider.GetService<IWorkflowHost>();
            host.RegisterWorkflow<ActivityWorkflow, MyData>();
            host.Start();

            Console.WriteLine("Starting workflow...");

            var workflowId = host.StartWorkflow("activity-sample", new MyData() { Request = "Spend $1,000,000" }).Result;

            var approval = host.GetPendingActivity("get-approval", "worker1", TimeSpan.FromMinutes(1)).Result;

            if (approval != null)
            {                
                Console.WriteLine("Approval required for " + approval.Parameters);
                host.SubmitActivitySuccess(approval.Token, "John Smith");
            }

            Console.ReadLine();
            host.Stop();
        }

        private static IServiceProvider ConfigureServices()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddWorkflow();
            //services.AddWorkflow(x => x.UseMongoDB(@"mongodb://localhost:27017", "workflow"));
            //services.AddWorkflow(x => x.UseSqlServer(@"Server=.;Database=WorkflowCore;Trusted_Connection=True;", true, true));
            //services.AddWorkflow(x => x.UsePostgreSQL(@"Server=127.0.0.1;Port=5432;Database=workflow;User Id=postgres;", true, true));
            services.AddLogging(cfg => 
            {
                cfg.AddConsole();
                cfg.AddDebug();
            });

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }
    }
}
