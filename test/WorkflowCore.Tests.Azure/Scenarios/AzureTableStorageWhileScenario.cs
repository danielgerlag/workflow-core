using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.Azure.Scenarios
{
    [Collection("AzureTableStorage collection")]
    public class AzureTableStorageWhileScenario : WhileScenario
    {        
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UseAzureTableStoragePersistence(AzureTableStorageDockerSetup.ConnectionString, "TestWorkflows"));
        }
    }
}