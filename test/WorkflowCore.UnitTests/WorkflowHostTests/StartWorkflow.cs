using Machine.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore.TestAssets.Workflows.HelloWorld;

namespace WorkflowCore.UnitTests.WorkflowHostTests
{
    [Subject(typeof(WorkflowHost))]
    public class StartWorkflow
    {
        Establish context = () =>
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddWorkflow();
            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddDebug();

            Subject = serviceProvider.GetService<IWorkflowHost>();
            Subject.RegisterWorkflow<HelloWorldWorkflow>();
            Subject.Start();
        };

        Because of = () => workflowId = Subject.StartWorkflow("HelloWorld", 1, null).Result;

        It should_have_an_id = () => workflowId.ShouldNotBeNull();


        Cleanup after = () =>
        {
            Subject.Stop();
        };

        static IWorkflowHost Subject;
        static string workflowId;

        

    }
}
