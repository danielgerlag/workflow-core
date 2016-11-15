using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore.Services;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.TestHost.CustomSteps;
using WorkflowCore.TestHost.CustomData;
using MongoDB.Driver;
using WorkflowCore.Persistence.MongoDB.Services;
using WorkflowCore.TestHost.Workflows;

namespace WorkflowCore.TestHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            //services.AddWorkflow(x => x.UsePersistence(sp => new MemoryPersistenceProvider()));
            services.AddWorkflow(x => x.UsePersistence(sp =>
            {
                var client = new MongoClient(@"mongodb://localhost:27017");
                var db = client.GetDatabase("workflow");
                return new MongoPersistenceProvider(db);
            }));

            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddConsole(LogLevel.Debug, true);
            loggerFactory.AddDebug();

            //start the workflow runtime
            var runtime = serviceProvider.GetService<IWorkflowRuntime>();
            var registry = serviceProvider.GetService<IWorkflowRegistry>();
            
            registry.RegisterWorkflow(new SimpleDecisionWorkflow());
            registry.RegisterWorkflow(new PassingDataWorkflow());
            registry.RegisterWorkflow(new EventSampleWorkflow());
            runtime.StartRuntime();

            //HelloWorldWorkflow(registry, runtime);
            //SimpleDecisionWorkflow(registry, runtime);
            //PassingDataSample(registry, runtime);
            EventSample(registry, runtime);

            runtime.StopRuntime();
        }

        
        private static void SimpleDecisionWorkflow(IWorkflowRegistry registry, IWorkflowRuntime runtime)
        {   
            runtime.StartWorkflow("Simple Decision Workflow", 1, null);
            Console.ReadLine();
        }

        private static void PassingDataSample(IWorkflowRegistry registry, IWorkflowRuntime runtime)
        {            
            MyDataClass initialData = new MyDataClass();
            initialData.Value1 = 2;
            initialData.Value2 = 3;
            runtime.StartWorkflow("PassingDataWorkflow", 1, initialData);
            Console.ReadLine();
        }

        private static void EventSample(IWorkflowRegistry registry, IWorkflowRuntime runtime)
        {            
            runtime.StartWorkflow("EventSampleWorkflow", 1, new MyDataClass());
            
            Console.WriteLine("Enter value");
            string value = Console.ReadLine();
            runtime.PublishEvent("MyEvent", "0", value);
            Console.ReadLine();
        }

    }
}
