using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.SqlServer.Scenarios
{
    [Collection(SqlServerCollection.Name)]
    public class SqlServerDelayScenario : DelayScenario
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(cfg =>
            {
                cfg.UseSqlServer(SqlDockerSetup.ScenarioConnectionString, true, true);
                cfg.UsePollInterval(TimeSpan.FromSeconds(2));
            });
        }
    }

    [Collection(SqlServerCollection.Name)]
    public class OptimizedSqlServerDelayScenario : DelayScenario
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(cfg =>
            {
                cfg.UseSqlServer(SqlDockerSetup.ScenarioConnectionString, true, true, true);
                cfg.UsePollInterval(TimeSpan.FromSeconds(2));
            });
        }
    }
}
