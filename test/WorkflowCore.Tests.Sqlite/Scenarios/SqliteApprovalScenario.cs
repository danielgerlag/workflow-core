using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;
using Xunit.Abstractions;

namespace WorkflowCore.Tests.Sqlite.Scenarios
{
    [Collection("Sqlite collection")]
    public class SqliteApprovalScenario : ApprovalScenario
    {
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