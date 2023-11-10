using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.SqlServer.Scenarios
{
    [Collection("SqlServer collection")]
    public class SqlServerPurgeScenario : WorkflowPurgeScenario
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(options =>
            {
                options.SetEventsPurgerOptions(new Models.EventsPurgerOptions(1));
                options.UseSqlServer(SqlDockerSetup.ScenarioConnectionString, true, true);
            });
        }

        [Fact]
        public Task RunAsync()
        {
            return ScenarioAsync();
        }
    }
}
