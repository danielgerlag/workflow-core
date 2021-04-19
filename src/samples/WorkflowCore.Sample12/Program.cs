﻿using Microsoft.Extensions.DependencyInjection;
using System;
using WorkflowCore.Interface;

namespace WorkflowCore.Sample12
{
    class Program
    {
        public static void Main(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            //start the workflow host
            var host = serviceProvider.GetService<IWorkflowHost>();
            host.RegisterWorkflow<OutcomeWorkflow, MyData>();
            host.Start();

            Console.WriteLine("Starting workflow...");
            host.StartWorkflow("outcome-sample", new MyData { Value = 2 });


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
            //services.AddWorkflow(x => x.UseSqlServer(@"Server=.\SQLEXPRESS;Database=WorkflowCoreTest001;Trusted_Connection=True;", true, true));
            //services.AddWorkflow(x => x.UseSqlite(@"Data Source=wfc001.db;", true));            


            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }
    }
}