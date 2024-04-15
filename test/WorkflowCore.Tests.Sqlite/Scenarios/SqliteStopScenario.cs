using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using WorkflowCore.Tests.Sqlite;
using Xunit;

namespace WorkflowCore.Tests.Sqlite.Scenarios
{
    [Collection("Sqlite collection")]
    public class SqliteStopScenario : StopScenario
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UseSqlite(SqliteSetup.ConnectionString, true));
        }
    }
}
