using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using WorkflowCore.Persistence.Oracle;

using Xunit;
using Xunit.Abstractions;

namespace WorkflowCore.Tests.Oracle.Scenarios
{
    [Collection("Oracle collection")]
    public class OracleDelayScenario : DelayScenario
    {
        public OracleDelayScenario(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(cfg =>
            {
                cfg.UseOracle(OracleDockerSetup.ConnectionString, true, true);
                cfg.UsePollInterval(TimeSpan.FromSeconds(2));
            });
        }
    }
}
