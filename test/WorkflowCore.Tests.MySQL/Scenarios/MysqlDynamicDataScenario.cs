using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.MySQL.Scenarios
{
    [Collection("Mysql collection")]
    public class MysqlDynamicDataScenario : DynamicDataIOScenario
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UseMySQL(MysqlDockerSetup.ScenarioConnectionString, true, true));
        }
    }
}
