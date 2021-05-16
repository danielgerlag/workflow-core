using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using WorkflowCore.Interface;

namespace WorkflowCore.Sample08
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            //start the workflow host
            var host = serviceProvider.GetService<IWorkflowHost>();
            host.RegisterWorkflow<HumanWorkflow>();
            host.Start();


            Console.WriteLine("Starting workflow...");
            string workflowId = host.StartWorkflow("HumanWorkflow").Result;

            var timer = new Timer(new TimerCallback((state) => { PrintOptions(host, workflowId); }), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            Thread.Sleep(1000);
            Console.WriteLine();
            Console.WriteLine("Open user actions are");
            var openItems = host.GetOpenUserActions(workflowId);
            foreach (var item in openItems)
            {
                Console.WriteLine(item.Prompt + ", Assigned to " + item.AssignedPrincipal);
                Console.WriteLine("Options are ");
                foreach (var option in item.Options)
                {
                    Console.WriteLine(" - " + option.Key + " : " + option.Value + ", ");
                }

                //Thread.Sleep(500);

                var input = Console.ReadLine();
                Console.WriteLine();

                string key = item.Key;
                string value = item.Options.Single(x => x.Value == input).Value;

                Console.WriteLine("Choosing key:" + key + " value:" + value);

                host.PublishUserAction(key, @"domain\john", value).Wait();
            }
            Thread.Sleep(1000);
            Console.WriteLine("Open user actions left:" + host.GetOpenUserActions(workflowId).Count().ToString());
            timer.Dispose();
            timer = null;
            Console.WriteLine("Workflow ended.");
            Console.ReadLine();
            host.Stop();
        }

        private static void PrintOptions(IWorkflowHost host, string workflowId)
        {
            var openItems = host.GetOpenUserActions(workflowId);
            foreach (var item in openItems)
            {
                Console.WriteLine(item.Prompt + ", Assigned to " + item.AssignedPrincipal);
                Console.WriteLine("Options are ");
                foreach (var option in item.Options)
                {
                    Console.WriteLine(" - " + option.Key + " : " + option.Value + ", ");
                }
            }
        }

        private static IServiceProvider ConfigureServices()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddWorkflow();
            //services.AddWorkflow(x => x.UseMongoDB(@"mongodb://localhost:27017", "workflow3"));
            //services.AddWorkflow(x => x.UseSqlServer(@"Server=.;Database=WorkflowCore3;Trusted_Connection=True;", true, true));            
            //services.AddWorkflow(x => x.UseSqlite(@"Data Source=database2.db;", true));            


            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }


    }
}
