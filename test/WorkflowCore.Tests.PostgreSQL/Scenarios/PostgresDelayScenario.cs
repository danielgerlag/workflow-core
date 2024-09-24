using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.PostgreSQL.Scenarios
{
    [Collection(PostgresCollection.Name)]
    public class PostgresDelayScenario : DelayScenario
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(cfg =>
            {
                cfg.UsePostgreSQL(PostgresDockerSetup.ScenarioConnectionString, true, true);
                cfg.UsePollInterval(TimeSpan.FromSeconds(2));
            });
        }
    }

    [Collection(PostgresCollection.Name)]
    public class OptimizedPostgresDelayScenario : DelayScenario
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(cfg =>
            {
                cfg.UsePostgreSQL(PostgresDockerSetup.ScenarioConnectionString, true, true, true);
                cfg.UsePollInterval(TimeSpan.FromSeconds(2));
            });
        }
    }
}
