using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;
using Xunit.Abstractions;

namespace WorkflowCore.Tests.SqlServer.Scenarios
{
    [Collection("SqlServer collection")]
    public class SqlServerDelayScenario : DelayScenario
    {
        public SqlServerDelayScenario(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(cfg =>
            {
                cfg.UseSqlServer(SqlDockerSetup.ScenarioConnectionString, true, true);
                cfg.UsePollInterval(TimeSpan.FromSeconds(2));
            });
        }
    }
}
