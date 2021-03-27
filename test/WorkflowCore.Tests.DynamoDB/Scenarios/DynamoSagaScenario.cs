using System;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.DynamoDB.Scenarios
{
    [Collection("DynamoDb collection")]
    public class DynamoSagaScenario : SagaScenario
    {        
        protected override void ConfigureServices(IServiceCollection services)
        {
            var cfg = new AmazonDynamoDBConfig {ServiceURL = DynamoDbDockerSetup.ConnectionString};
            services.AddWorkflow(x => x.UseAwsDynamoPersistence(DynamoDbDockerSetup.Credentials, cfg, "tests-"));
        }
    }
}
