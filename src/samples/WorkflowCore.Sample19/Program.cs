using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Logging.Console;
using WorkflowCore.Interface;
using WorkflowCore.Sample19.Middleware;
using WorkflowCore.Sample19.Steps;

namespace WorkflowCore.Sample19
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = ConfigureServices();

            // Start the workflow host
            var host = serviceProvider.GetService<IWorkflowHost>();
            host.RegisterWorkflow<FlakyConnectionWorkflow, FlakyConnectionParams>();
            host.Start();

            var workflowParams = new FlakyConnectionParams
            {
                Description = "Flaky connection workflow"
            };
            var workflowId = host.StartWorkflow("flaky-sample", workflowParams).Result;
            Console.WriteLine($"Kicked off workflow {workflowId}");

            Console.ReadLine();
            host.Stop();
        }

        private static IServiceProvider ConfigureServices()
        {
            // Setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddWorkflow();

            // Add step middleware
            // Note that middleware will get executed in the order in which they were registered
            services.AddWorkflowStepMiddleware<AddMetadataToLogsMiddleware>();
            services.AddWorkflowStepMiddleware<PollyRetryMiddleware>();

            // Add some pre workflow middleware
            // This middleware will run before the workflow starts
            services.AddWorkflowMiddleware<AddDescriptionWorkflowMiddleware>();

            // Add some post workflow middleware
            // This middleware will run after the workflow completes
            services.AddWorkflowMiddleware<PrintWorkflowSummaryMiddleware>();

            // Add workflow steps
            services.AddTransient<LogMessage>();
            services.AddTransient<FlakyConnection>();

            services.AddLogging(cfg =>
            {
                cfg.AddConsole(x => x.IncludeScopes = true);
                cfg.AddDebug();
            });

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }
    }
}
