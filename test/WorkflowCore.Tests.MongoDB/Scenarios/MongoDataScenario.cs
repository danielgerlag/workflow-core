using System;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.MongoDB.Scenarios
{
    [Collection("Mongo collection")]
    public class MongoDataScenario : DataIOScenario
    {
        public MongoDataScenario() : base()
        {
            BsonClassMap.RegisterClassMap<MyDataClass>(map => map.AutoMap());
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UseMongoDB(MongoDockerSetup.ConnectionString, nameof(MongoDataScenario)));
        }
    }
}
