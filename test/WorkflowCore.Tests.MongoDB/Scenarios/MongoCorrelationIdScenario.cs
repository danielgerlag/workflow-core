using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.MongoDB.Scenarios
{
    [Collection("Mongo collection")]
    public class MongoCorrelationIdScenario : CorrelationIdScenario
    {        
        protected override void ConfigureServices(IServiceCollection services)
        {
            // Should be "integration-tests".
            // But MongoPersistenceProvider uses static indexesCreated
            // so if we have two databases in same process then second database is created without indexes.
            services.AddWorkflow(x => x.UseMongoDB(MongoDockerSetup.ConnectionString, "workflow-tests"));
        }
    }
}
