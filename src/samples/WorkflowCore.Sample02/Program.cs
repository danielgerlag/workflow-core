﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using WorkflowCore.Interface;


namespace WorkflowCore.Sample02
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            //start the workflow host
            var host = serviceProvider.GetService<IWorkflowHost>();
            host.RegisterWorkflow<SimpleDecisionWorkflow>();
            host.Start();

            host.StartWorkflow("Simple Decision Workflow");

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
