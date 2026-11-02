using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;
using Xunit.Abstractions;

namespace WorkflowCore.Tests.PostgreSQL.Scenarios
{
    [Collection("Postgres collection")]
    public class PostgresDelayScenario : DelayScenario
    {
        public PostgresDelayScenario(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(cfg =>
            {
                cfg.UsePostgreSQL(PostgresDockerSetup.ScenarioConnectionString, true, true);
                cfg.UsePollInterval(TimeSpan.FromSeconds(2));
            });
        }
    }
}
