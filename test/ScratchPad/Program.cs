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
            var loader = serviceProvider.GetService<IDefinitionLoader>();
            var str = ScratchPad.Properties.Resources.HelloWorld; //Encoding.UTF8.GetString(ScratchPad.Properties.Resources.HelloWorld);

            loader.LoadDefinition(str);

            //host.RegisterWorkflow<HelloWorldWorkflow>();
            host.Start();

            host.StartWorkflow("HelloWorld", 1, null);

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

    public class MyDataClass
    {
        public int Value1 { get; set; }

        public int Value2 { get; set; }

        public int Value3 { get; set; }
    }
}

