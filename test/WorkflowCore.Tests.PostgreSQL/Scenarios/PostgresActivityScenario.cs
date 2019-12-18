using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.PostgreSQL.Scenarios
{
    [Collection("Postgres collection")]
    public class PostgresActivityScenario : ActivityScenario
    {        
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UsePostgreSQL(PostgresDockerSetup.ScenarioConnectionString, true, true));
        }
    }
}
