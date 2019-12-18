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
using WorkflowCore.Models.Search;

namespace ScratchPad
{
    public class ElasticTest
    {
        public static void test(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            //start the workflow host
            var host = serviceProvider.GetService<IWorkflowHost>();
            var searchIndex = serviceProvider.GetService<ISearchIndex>();

            host.RegisterWorkflow<WorkflowCore.Sample03.PassingDataWorkflow, WorkflowCore.Sample03.MyDataClass>();
            host.RegisterWorkflow<WorkflowCore.Sample04.EventSampleWorkflow, WorkflowCore.Sample04.MyDataClass>();

            host.Start();
            var data1 = new WorkflowCore.Sample03.MyDataClass() { Value1 = 2, Value2 = 3 };
            host.StartWorkflow("PassingDataWorkflow", data1, "quick dog").Wait();

            var data2 = new WorkflowCore.Sample04.MyDataClass() { Value1 = "test" };
            host.StartWorkflow("EventSampleWorkflow", data2, "alt1 boom").Wait();


            var searchResult1 = searchIndex.Search("dog", 0, 10).Result;
            var searchResult2 = searchIndex.Search("quick dog", 0, 10).Result;
            var searchResult3 = searchIndex.Search("fast", 0, 10).Result;
            var searchResult4 = searchIndex.Search("alt1", 0, 10).Result;
            var searchResult5 = searchIndex.Search("dogs", 0, 10).Result;
            var searchResult6 = searchIndex.Search("test", 0, 10).Result;
            var searchResult7 = searchIndex.Search("", 0, 10).Result;
            var searchResult8 = searchIndex.Search("", 0, 10, ScalarFilter.Equals(x => x.Reference, "quick dog")).Result;
            var searchResult9 = searchIndex.Search("", 0, 10, ScalarFilter.Equals<WorkflowCore.Sample03.MyDataClass>(x => x.Value1, 2)).Result;

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

            services.AddTransient<WorkflowCore.Sample01.Steps.GoodbyeWorld>();

            var serviceProvider = services.BuildServiceProvider();


            return serviceProvider;
        }

    }
        
}

