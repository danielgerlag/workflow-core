using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using System.Text;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Nest;

namespace ScratchPad
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            //start the workflow host
            var host = serviceProvider.GetService<IWorkflowHost>();
            var searchIndex = serviceProvider.GetService<ISearchIndex>();

            host.RegisterWorkflow<WorkflowCore.Sample01.HelloWorldWorkflow>();

            host.Start();
            //host.StartWorkflow<object>("HelloWorld", null, "ref1").Wait();

            var searchResult = searchIndex.Search("ref2", 0, 10).Result;

            Console.ReadLine();
            host.Stop();
        }

        private static IServiceProvider ConfigureServices()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            //services.AddWorkflow();
            //services.AddWorkflow(x => x.UseMongoDB(@"mongodb://localhost:27017", "workflow"));
            services.AddWorkflow(cfg =>
            {
                //var ddbConfig = new AmazonDynamoDBConfig() { RegionEndpoint = RegionEndpoint.USWest2 };
                //cfg.UseAwsDynamoPersistence(new EnvironmentVariablesAWSCredentials(), ddbConfig, "elastic");
                cfg.UseElasticsearch(new ConnectionSettings(new Uri("http://localhost:9200")), "workflows");
                //cfg.UseAwsSimpleQueueService(new EnvironmentVariablesAWSCredentials(), new AmazonSQSConfig() { RegionEndpoint = RegionEndpoint.USWest2 });
                //cfg.UseAwsDynamoLocking(new EnvironmentVariablesAWSCredentials(), new AmazonDynamoDBConfig() { RegionEndpoint = RegionEndpoint.USWest2 }, "workflow-core-locks");
            });


            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddDebug();
            return serviceProvider;
        }

    }
        
}

