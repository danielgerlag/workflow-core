using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using WorkflowCore.Persistence.Oracle;

using Xunit;

namespace WorkflowCore.Tests.Oracle.Scenarios
{
    [Collection("Oracle collection")]
    public class OracleForkScenario : ForkScenario<OracleForkScenario>
    {
        protected override void Configure(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UseOracle(OracleDockerSetup.ConnectionString, true, true));
        }
    }
}
