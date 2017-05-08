using Machine.Specifications;
using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.IntegrationTests.Scenarios;
using WorkflowCore.Services;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Models;

namespace WorkflowCore.Tests.MongoDB.Scenarios
{
    [Subject(typeof(WorkflowHost))]
    public class Mongo_ExternalEvents : ExternalEventsTest
    {
        protected override void ConfigureWorkflow(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UseMongoDB($"mongodb://localhost:{DockerSetup.Port}", "workflow-tests"));
        }

        Behaves_like<ExternalEventsBehavior> a_external_events_workflow;
    }
}
