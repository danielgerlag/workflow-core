using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.MySQL.Scenarios
{
    [Collection("Mysql collection")]
    public class MysqlPurgeScenario : WorkflowPurgeScenario
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(options =>
            {
                options.SetEventsPurgerOptions(new Models.EventsPurgerOptions(1));
                options.UseMySQL(MysqlDockerSetup.ScenarioConnectionString, true, true);
            });
        }

        [Fact]
        public Task RunAsync()
        {
            return ScenarioAsync();
        }
    }
}
