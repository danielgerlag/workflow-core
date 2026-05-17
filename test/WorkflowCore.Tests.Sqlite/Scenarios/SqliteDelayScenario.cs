using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;
using Xunit.Abstractions;

namespace WorkflowCore.Tests.Sqlite.Scenarios
{
    [Collection("Sqlite collection")]
    public class SqliteDelayScenario : DelayScenario
    {
        public SqliteDelayScenario(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(cfg =>
            {
                cfg.UseSqlite($"Data Source=wfc-tests-{DateTime.Now.Ticks}.db;", true);
                cfg.UsePollInterval(TimeSpan.FromSeconds(2));
            });
        }
    }
}

