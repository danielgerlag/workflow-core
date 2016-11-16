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

namespace WorkflowCore.Sample05
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            //start the workflow runtime
            var runtime = serviceProvider.GetService<IWorkflowRuntime>();
            runtime.RegisterWorkflow<DeferSampleWorkflow>();
            runtime.StartRuntime();

            runtime.StartWorkflow("DeferSampleWorkflow", 1, null);
                        
            Console.ReadLine();
            runtime.StopRuntime();
        }

        private static IServiceProvider ConfigureServices()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddWorkflow(wf =>
            {
                wf.UsePersistence(sp => new MemoryPersistenceProvider());
                wf.UseThreads(1);
            });

            //services.AddWorkflow(wf =>
            //{
            //    wf.UseThreads(1);
            //    wf.UsePersistence(sp =>
            //    {
            //        var client = new MongoClient(@"mongodb://localhost:27017");
            //        var db = client.GetDatabase("workflow");
            //        return new MongoPersistenceProvider(db);
            //    });
            //});


            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddDebug();
            return serviceProvider;
        }
    }
}
