#region using

using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using WorkflowCore.Interface;
using WorkflowCore.SampleSqlServer.Steps;

#endregion

namespace WorkflowCore.SampleSqlServer
{
    class Program
    {
        private static readonly string _connectionString = "Server=(local);Database=wfc;User Id=wfc;Password=wfc;";
        private static ILogger<Program> _lg;

        public static void Main(string[] args)
        {
            IServiceProvider serviceProvider = ConfigureServices();

            _lg = serviceProvider.GetService<ILoggerFactory>().CreateLogger<Program>();
            _lg.LogDebug("->");

            //start the workflow host
            var host = serviceProvider.GetService<IWorkflowHost>();
            host.RegisterWorkflow<HelloWorldWorkflow, HelloWorldData>();
            host.Start();


            while (true)
            {
                Console.WriteLine("\nS:start E:event: Q:quit");
                var key = Console.ReadKey(true);
                switch (key.KeyChar)
                {
                    case 'S':
                        var id = host.StartWorkflow("HelloWorld", 1, new HelloWorldData {ID=123});
                        Console.WriteLine(id.Result);
                        break;

                    case 'E':
                        Console.Write("EventID:");
                        var idIn = Console.ReadLine();
                        host.PublishEvent("Go", "0", int.Parse(idIn));
                        break;

                    case 'Q':
                        host.Stop();
                        Environment.Exit(0);
                        break;

                }
            }
        }

        private static IServiceProvider ConfigureServices()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();

            //services.AddWorkflow();

            services.AddWorkflow(x =>
                {
                    x.UseSqlServer(_connectionString, false, true);
                    x.UseSqlServerLocking(_connectionString);
                    x.UseSqlServerQueue(_connectionString, true);
                }
            );

            services.AddTransient<GoodbyeWorld>();

            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddDebug(LogLevel.Debug);

            return serviceProvider;
        }
    }
}