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
using WorkflowCore.Services.DefinitionStorage;

namespace ScratchPad
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            //start the workflow host
            var host = serviceProvider.GetService<IWorkflowHost>();
            var loader = serviceProvider.GetService<IDefinitionLoader>();
            var activityController = serviceProvider.GetService<IActivityController>();
            //host.RegisterWorkflow<Test01Workflow, WfData>();
            loader.LoadDefinition(Properties.Resources.HelloWorld, Deserializers.Json);
            
            host.Start();
            
            host.StartWorkflow("Test02", 1, new WfData()
            {
                Value1 = "data1",
                Value2 = "data2"
            });

            var act = activityController.GetPendingActivity("act1", "worker1", TimeSpan.FromMinutes(1)).Result;

            if (act != null)
            {
                Console.WriteLine("get act " + act.Token);
                Console.WriteLine(act.Parameters);

                activityController.SubmitActivitySuccess(act.Token, "BOO");
            }
            
            
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
                //cfg.UseElasticsearch(new ConnectionSettings(new Uri("http://localhost:9200")), "workflows");
                //cfg.UseAwsSimpleQueueService(new EnvironmentVariablesAWSCredentials(), new AmazonSQSConfig() { RegionEndpoint = RegionEndpoint.USWest2 });
                //cfg.UseAwsDynamoLocking(new EnvironmentVariablesAWSCredentials(), new AmazonDynamoDBConfig() { RegionEndpoint = RegionEndpoint.USWest2 }, "workflow-core-locks");
            });

            
            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }

    }
    
    public class HelloWorld : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine("Hello world");
            return ExecutionResult.Next();
        }
    }
    
    public class CustomMessage : StepBody
    {
        
        public string Message { get; set; }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine(Message);
            return ExecutionResult.Next();
        }
    }
     
    public class WfData
    {
        public string Value1 { get; set; }
        public string Value2 { get; set; }
        public string Value3 { get; set; }
    } 
    
    public class Test01Workflow : IWorkflow<WfData>
    {
        public void Build(IWorkflowBuilder<WfData> builder)
        {
            builder                
                .StartWith<HelloWorld>()
                .Activity("act1", (data) => data.Value1)
                    .Output(data => data.Value3, step => step.Result)
                .Then<CustomMessage>()
                    .Input(step => step.Message, data => data.Value3);
        }

        public string Id => "Test01";
            
        public int Version => 1;
                 
    }
    
}

