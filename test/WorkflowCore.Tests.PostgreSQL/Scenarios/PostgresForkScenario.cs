using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.PostgreSQL.Scenarios
{
    [Collection(PostgresCollection.Name)]
    public class PostgresForkScenario : ForkScenario<PostgresForkScenario>
    {
        protected override void Configure(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UsePostgreSQL(PostgresDockerSetup.ScenarioConnectionString, true, true));
        }
    }

    [Collection(PostgresCollection.Name)]
    public class OptimizedPostgresForkScenario : ForkScenario<OptimizedPostgresForkScenario>
    {
        protected override void Configure(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UsePostgreSQL(PostgresDockerSetup.ScenarioConnectionString, true, true, true));
        }
    }
}
