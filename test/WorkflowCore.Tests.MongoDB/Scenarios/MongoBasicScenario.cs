﻿using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.MongoDB.Scenarios
{
    [Collection("Mongo collection")]
    public class MongoBasicScenario : BasicScenario
    {        
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UseMongoDB(MongoDockerSetup.ConnectionString, "integration-tests"));
        }
    }
}
