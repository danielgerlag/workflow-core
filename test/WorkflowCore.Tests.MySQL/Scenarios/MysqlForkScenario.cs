using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.MySQL.Scenarios
{
    [Collection("Mysql collection")]
    [Obsolete]
    public class MysqlForkScenario : ForkScenario
    {        
        protected override void Configure(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UseMySQL(MysqlDockerSetup.ScenarioConnectionString, true, true));
        }
    }
}
