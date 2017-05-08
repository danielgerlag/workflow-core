using Machine.Specifications;
using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.IntegrationTests.Scenarios;
using WorkflowCore.Services;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Models;

namespace WorkflowCore.Tests.PostgreSQL.Scenarios
{
    [Subject(typeof(WorkflowHost))]
    public class PostgreSQL_ExternalEvents : ExternalEventsTest
    {
        protected override void ConfigureWorkflow(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UsePostgreSQL($"Server=127.0.0.1;Port={DockerSetup.Port};Database=workflow;User Id=postgres;", true, true));
        }

        Behaves_like<ExternalEventsBehavior> a_external_events_workflow;
    }
}
