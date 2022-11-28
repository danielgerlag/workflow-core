using System;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.MongoDB.Scenarios
{
    [Collection("Mongo collection")]
    public class MongoDelayScenario : DelayScenario
    {
        public MongoDelayScenario() : base()
        {
            BsonClassMap.RegisterClassMap<DelayWorkflow.MyDataClass>(map => map.AutoMap());
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(cfg =>
            {
                cfg.UseMongoDB(MongoDockerSetup.ConnectionString, nameof(MongoDelayScenario));
                cfg.UsePollInterval(TimeSpan.FromSeconds(2));
            });
        }
    }
}
