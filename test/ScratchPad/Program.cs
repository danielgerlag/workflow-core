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

namespace ScratchPad
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //var s = typeof(HelloWorld).AssemblyQualifiedName;


            IServiceProvider serviceProvider = ConfigureServices();

            //start the workflow host
            var host = serviceProvider.GetService<IWorkflowHost>();
            var persistence = serviceProvider.GetService<IPersistenceProvider>();

            var wf = new WorkflowInstance()
            {
                Description = "test",
                CreateTime = DateTime.UtcNow,
                Status = WorkflowStatus.Terminated,
                Version = 1,
                WorkflowDefinitionId = "def"
            };
            var id = persistence.CreateNewWorkflow(wf).Result;

            //var loader = serviceProvider.GetService<IDefinitionLoader>();
            //var str = ScratchPad.Properties.Resources.HelloWorld; //Encoding.UTF8.GetString(ScratchPad.Properties.Resources.HelloWorld);
            //loader.LoadDefinition(str);
            //host.RegisterWorkflow<HelloWorldWorkflow>();
            //host.Start();

            //host.StartWorkflow("HelloWorld", 1, new MyDataClass() { Value3 = "hi there" });


            Console.ReadLine();
            //host.Stop();
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
                var ddbConfig = new AmazonDynamoDBConfig() { RegionEndpoint = RegionEndpoint.USWest2 };
                cfg.UseAwsDynamoPersistence(new EnvironmentVariablesAWSCredentials(), ddbConfig, "sample31");
                //cfg.UseAwsSimpleQueueService(new EnvironmentVariablesAWSCredentials(), new AmazonSQSConfig() { RegionEndpoint = RegionEndpoint.USWest2 });
                //cfg.UseAwsDynamoLocking(new EnvironmentVariablesAWSCredentials(), new AmazonDynamoDBConfig() { RegionEndpoint = RegionEndpoint.USWest2 }, "workflow-core-locks");
            });


            services.AddTransient<GoodbyeWorld>();

            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddDebug();
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
    public class GoodbyeWorld : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine("Goodbye world");
            return ExecutionResult.Next();
        }
    }

    public class Throw : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine("throwing...");
            throw new Exception("up");
        }
    }

    public class PrintMessage : StepBody
    {
        public string Message { get; set; }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine(Message);
            return ExecutionResult.Next();
        }
    }

    public class GenerateMessage : StepBody
    {
        public string Message { get; set; }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Message = "Generated message";
            return ExecutionResult.Next();
        }
    }

    public class MyDataClass
    {
        public int Value1 { get; set; }

        public int Value2 { get; set; }

        public string Value3 { get; set; }
    }
}

