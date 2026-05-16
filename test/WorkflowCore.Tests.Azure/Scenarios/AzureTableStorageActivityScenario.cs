using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.Azure.Scenarios
{
    [Collection("AzureTableStorage collection")]
    public class AzureTableStorageActivityScenario : ActivityScenario
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UseAzureTableStoragePersistence(AzureTableStorageDockerSetup.ConnectionString, "TestActivity"));
        }
    }
}
