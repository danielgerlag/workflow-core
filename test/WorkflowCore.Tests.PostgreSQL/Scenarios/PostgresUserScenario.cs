using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.PostgreSQL.Scenarios
{
    [Collection(PostgresCollection.Name)]
    public class PostgresUserScenario : UserScenario<PostgresUserScenario>
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UsePostgreSQL(PostgresDockerSetup.ScenarioConnectionString, true, true));
        }
    }

    [Collection(PostgresCollection.Name)]
    public class OptimizedPostgresUserScenario : UserScenario<OptimizedPostgresUserScenario>
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UsePostgreSQL(PostgresDockerSetup.ScenarioConnectionString, true, true, true));
        }
    }
}
