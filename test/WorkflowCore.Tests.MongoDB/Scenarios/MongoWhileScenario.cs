﻿using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.MongoDB.Scenarios
{
    [Collection("Mongo collection")]
    public class MongoWhileScenario : WhileScenario<MongoWhileScenario>
    {        
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UseMongoDB(MongoDockerSetup.ConnectionString, nameof(MongoWhileScenario)));
        }
    }
}
