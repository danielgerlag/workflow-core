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
    public class Program
    {
        public static void Main(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            //start the workflow host
            var host = serviceProvider.GetService<IWorkflowHost>();
            var searchIndex = serviceProvider.GetService<ISearchIndex>();

            host.RegisterWorkflow<WorkflowCore.Sample03.PassingDataWorkflow, WorkflowCore.Sample03.MyDataClass>();
            host.RegisterWorkflow<WorkflowCore.Sample04.EventSampleWorkflow, WorkflowCore.Sample04.MyDataClass>();

            host.Start();
            //var data = new WorkflowCore.Sample03.MyDataClass() { ValueStr = "blue moon", Value1 = 2, Value2 = 3 };
            //host.StartWorkflow<object>("PassingDataWorkflow", data, "pass2").Wait();

            //var data = new WorkflowCore.Sample04.MyDataClass() { StrValue = "test" };
            //host.StartWorkflow("EventSampleWorkflow", data, "alt1").Wait();

            var searchResult1 = searchIndex.Search("ref1", 0, 10, DateRangeFilter.Between(x => x.CompleteTime, new DateTime(2018, 1, 1), new DateTime(2021, 1, 1))).Result;
            var searchResult2 = searchIndex.Search("ref2", 0, 10).Result;
            var searchResult3 = searchIndex.Search("PassingDataWorkflow", 0, 10).Result;
            var searchResult4 = searchIndex.Search("fox", 0, 10).Result;
            var searchResult5 = searchIndex.Search("dogs", 0, 10).Result;
            var searchResult6 = searchIndex.Search("", 0, 10).Result;
            var searchResult7 = searchIndex.Search("", 0, 10, ScalarFilter.Equals(x => x.Reference, "pass1")).Result;
            var searchResult8 = searchIndex.Search("", 0, 10, DateRangeFilter.Between(x => x.CompleteTime, new DateTime(2018, 1, 1), new DateTime(2021, 1, 1))).Result;
            var searchResult9 = searchIndex.Search("", 0, 10, ScalarFilter.Equals<WorkflowCore.Sample03.MyDataClass>(x => x.Data.ValueStr, "blue moon")).Result;
            var searchResult10 = searchIndex.Search("", 0, 10, ScalarFilter.Equals<WorkflowCore.Sample03.MyDataClass>(x => x.Data.Value1, 0)).Result;
            var searchResult11 = searchIndex.Search("", 0, 10, ScalarFilter.Equals<WorkflowCore.Sample04.MyDataClass>(x => x.Data.StrValue, "test")).Result;

            var searchResult12 = searchIndex.Search("", 0, 10, StatusFilter.Equals(WorkflowStatus.Runnable)).Result;
            var searchResult13 = searchIndex.Search("", 0, 10, StatusFilter.Equals(WorkflowStatus.Complete)).Result;

            var searchResult14 = searchIndex.Search("", 0, 10, NumericRangeFilter.LessThan<WorkflowCore.Sample03.MyDataClass>(x => x.Data.Value1, 6)).Result;

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

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddDebug();
            return serviceProvider;
        }

    }
        
}

