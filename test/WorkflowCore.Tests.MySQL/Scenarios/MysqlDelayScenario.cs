using Microsoft.Extensions.DependencyInjection;
using System;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.MySQL.Scenarios
{
    [Collection("Mysql collection")]
    public class MysqlDelayScenario : DelayScenario
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(cfg =>
            {
                cfg.UseMySQL(MysqlDockerSetup.ScenarioConnectionString, true, true);
                cfg.UsePollInterval(TimeSpan.FromSeconds(2));
            });
        }
    }
}
