using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;
using Xunit.Abstractions;

namespace WorkflowCore.Tests.SqlServer.Scenarios
{
    [Collection("SqlServer collection")]
    public class SqlServerApprovalScenario() : ApprovalScenario()
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UseSqlServer(SqlDockerSetup.ScenarioConnectionString, true, true));
        }
    }
}
