using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace WorkflowCore.Sample13
{
    class Program
    {
        public static void Main(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            //start the workflow host
            var host = serviceProvider.GetService<IWorkflowHost>();
            var controller = serviceProvider.GetService<IWorkflowController>();
            controller.RegisterWorkflow<ParallelWorkflow, MyData>();

            host.Start();

            Console.WriteLine("Starting workflow...");
            controller.StartWorkflow<MyData>("parallel-sample");
            
            Console.ReadLine();
            host.Stop();
        }

        private static IServiceProvider ConfigureServices()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddWorkflow();
            //services.AddWorkflow(x => x.UseMongoDB(@"mongodb://localhost:27017", "workflow-test009"));
            //services.AddWorkflow(x => x.UseSqlServer(@"Server=.\SQLEXPRESS;Database=WorkflowCoreTest007;Trusted_Connection=True;", true, true));
            //services.AddWorkflow(x => x.UseSqlite(@"Data Source=wfc001.db;", true));            

            //services.AddWorkflow(x =>
            //{
            //    x.UseAzureSynchronization(@"UseDevelopmentStorage=true");
            //    x.UseMongoDB(@"mongodb://localhost:27017", "workflow-test002");
            //});

            //services.AddWorkflow(x =>
            //{
            //    x.UseSqlServer(@"Server=.\SQLEXPRESS;Database=WorkflowCore3;Trusted_Connection=True;", true, true);
            //    x.UseSqlServerLocking(@"Server=.\SQLEXPRESS;Database=WorkflowCore3;Trusted_Connection=True;");
            //});

            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }
    }
}