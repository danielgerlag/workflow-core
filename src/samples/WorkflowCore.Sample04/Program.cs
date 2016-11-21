using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RabbitMQ.Client;
using StackExchange.Redis;
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

            //start the workflow host
            var host = serviceProvider.GetService<IWorkflowHost>();
            host.RegisterWorkflow<EventSampleWorkflow, MyDataClass>();
            host.Start();

            var initialData = new MyDataClass();
            host.StartWorkflow("EventSampleWorkflow", 1, initialData);

            Console.WriteLine("Enter value to publish");
            string value = Console.ReadLine();
            host.PublishEvent("MyEvent", "0", value);

            Console.ReadLine();
            host.Stop();
        }

        private static IServiceProvider ConfigureServices()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddWorkflow();
            //services.AddWorkflow(x => x.UseMongoDB(@"mongodb://localhost:27017", "workflow"));
            //services.AddWorkflow(x => x.UseSqlServer(@"Server=.;Database=WorkflowCore;Trusted_Connection=True;", true, true));
            //services.AddWorkflow(x => x.UsePostgreSQL(@"Server=127.0.0.1;Port=5432;Database=workflow;User Id=postgres;Password=password;", true, true));
            //services.AddWorkflow(x => x.UseSqlite(@"Data Source=database.db;", true));
            //redis = ConnectionMultiplexer.Connect("127.0.0.1");
            //services.AddWorkflow(x =>
            //{
            //    x.UseMongoDB(@"mongodb://localhost:27017", "workflow");
            //    x.UseRabbitMQ(new ConnectionFactory() { HostName = "localhost" });
            //    x.UseRedlock(redis);
            //});


            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddDebug();
            return serviceProvider;
        }

        private static IConnectionMultiplexer redis;
    }
}
