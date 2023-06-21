using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.PostgreSQL.Scenarios
{
    [Collection("Postgres collection")]
    public class PostgresPurgeScenario : WorkflowPurgeScenario
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(options => 
            {
                options.SetEventsPurgerOptions(new Models.EventsPurgerOptions(1));
                options.UsePostgreSQL(PostgresDockerSetup.ScenarioConnectionString, true, true);
            });
        }

        [Fact]
        public Task RunAsync()
        {
            return ScenarioAsync();
        }
    }
}
