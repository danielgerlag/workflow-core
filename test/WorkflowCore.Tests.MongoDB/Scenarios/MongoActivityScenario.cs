using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.MongoDB.Scenarios
{
    [Collection("Mongo collection")]
    public class MongoActivityScenario : ActivityScenario
    {        
        protected override void ConfigureServices(IServiceCollection services)
        {
            BsonClassMap.RegisterClassMap<ActivityScenario.ActivityInput>(x => x.AutoMap());
            BsonClassMap.RegisterClassMap<ActivityScenario.ActivityOutput>(x => x.AutoMap());

            services.AddWorkflow(x => x.UseMongoDB(MongoDockerSetup.ConnectionString, "integration-tests"));
        }
    }
}
