using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Persistence.MongoDB.Services;
using WorkflowCore.Services;

namespace WorkflowCore.Sample04
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            //start the workflow runtime
            var runtime = serviceProvider.GetService<IWorkflowRuntime>();
            runtime.RegisterWorkflow<EventSampleWorkflow, MyDataClass>();
            runtime.StartRuntime();

            var initialData = new MyDataClass();
            runtime.StartWorkflow("EventSampleWorkflow", 1, initialData);

            Console.WriteLine("Enter value to publish");
            string value = Console.ReadLine();
            runtime.PublishEvent("MyEvent", "0", value);

            Console.ReadLine();
            runtime.StopRuntime();
        }

        private static IServiceProvider ConfigureServices()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddWorkflow();
            //services.AddWorkflow(x => x.UseMongoDB(@"mongodb://localhost:27017", "workflow"));
            //services.AddWorkflow(x => x.UseSqlServer(@"Server=.;Database=WorkflowCore;Trusted_Connection=True;"));
            //services.AddWorkflow(x => x.UsePostgreSQL(@"Server=127.0.0.1;Port=5432;Database=workflow;User Id=postgres;Password=password;"));
            //services.AddWorkflow(x => x.UseSqlite(@"Data Source=database.db;"));

            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddDebug();
            return serviceProvider;
        }
    }
}
