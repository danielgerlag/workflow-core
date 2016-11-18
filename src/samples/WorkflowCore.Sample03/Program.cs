using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Services;


namespace WorkflowCore.Sample03
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            //start the workflow host
            var host = serviceProvider.GetService<IWorkflowHost>();
            host.RegisterWorkflow<PassingDataWorkflow, MyDataClass>();
            host.Start();

            var initialData = new MyDataClass();
            initialData.Value1 = 2;
            initialData.Value2 = 3;

            host.StartWorkflow("PassingDataWorkflow", 1, initialData);

            Console.ReadLine();
            host.Stop();
        }

        private static IServiceProvider ConfigureServices()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddWorkflow();
            //services.AddWorkflow(x => x.UseSqlServer(@"Server=.;Database=WorkflowCore;Trusted_Connection=True;"));
            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddDebug();
            return serviceProvider;
        }
    }
}
