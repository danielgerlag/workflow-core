using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.PostgreSQL.Scenarios
{
    [Collection("Postgres collection")]
    public class PostgresBasicScenario : BasicScenario
    {        
        protected override void Configure(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UsePostgreSQL($"Server=127.0.0.1;Port={DockerSetup.Port};Database=workflow;User Id=postgres;", true, true));
        }
    }
}
