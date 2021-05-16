using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.SqlServer.Scenarios
{
    [Collection("SqlServer collection")]
    public class SqlServerCompenstationScenario : CompensationScenario
    {        
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UseSqlServer(SqlDockerSetup.ScenarioConnectionString, true, true));
        }
    }
}
