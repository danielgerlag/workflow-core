using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Sample20.Steps;

namespace WorkflowCore.Sample20
{
    class Program
    {
        public static void Main(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            var host = serviceProvider.GetService<IWorkflowHost>();
            var persistence = serviceProvider.GetService<IPersistenceProvider>();
            host.RegisterWorkflow<ForkDemoWorkflow, BatchData>();
            host.Start();

            var eventKey = Guid.NewGuid().ToString();
            var workflowId = host.StartWorkflow("fork-demo", 1, new BatchData
            {
                Items = Enumerable.Range(1, 12).ToList(),
                Threshold = 5,
                EventKey = eventKey
            }).Result;

            Console.WriteLine($"Started workflow {workflowId} with 12 items");

            // Wait for the workflow to reach the WaitFor step
            WaitForEventSubscription(persistence, "ForkDecision", eventKey);

            // Fork the workflow from outside, mutating the data to split items
            var forkedId = host.ForkWorkflow(workflowId, data =>
            {
                var batch = (BatchData)data;
                batch.Items = batch.Items.Skip(batch.Threshold).ToList();
            }).Result;

            Console.WriteLine($"Forked workflow {forkedId} with overflow items");

            // Mutate the original to keep only the first N items
            var original = persistence.GetWorkflowInstance(workflowId).Result;
            var originalData = (BatchData)original.Data;
            originalData.Items = originalData.Items.Take(originalData.Threshold).ToList();
            persistence.PersistWorkflow(original).Wait();

            // Resume both by publishing events
            host.PublishEvent("ForkDecision", eventKey, null);

            // Wait for both to complete
            WaitForComplete(persistence, workflowId);
            WaitForComplete(persistence, forkedId);

            Console.WriteLine("Both workflows completed. Press enter to stop.");
            Console.ReadLine();
            host.Stop();
        }

        private static void WaitForEventSubscription(IPersistenceProvider persistence, string eventName, string eventKey)
        {
            for (int i = 0; i < 300; i++)
            {
                var subs = persistence.GetSubscriptions(eventName, eventKey, DateTime.MaxValue).Result;
                if (subs.Any())
                    return;
                Thread.Sleep(100);
            }
        }

        private static void WaitForComplete(IPersistenceProvider persistence, string workflowId)
        {
            for (int i = 0; i < 300; i++)
            {
                var instance = persistence.GetWorkflowInstance(workflowId).Result;
                if (instance.Status == WorkflowStatus.Complete)
                    return;
                Thread.Sleep(100);
            }
        }

        private static IServiceProvider ConfigureServices()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddLogging(cfg =>
            {
                cfg.AddConsole();
                cfg.AddDebug();
            });
            services.AddWorkflow();

            services.AddTransient<ProcessBatch>();

            return services.BuildServiceProvider();
        }
    }
}
