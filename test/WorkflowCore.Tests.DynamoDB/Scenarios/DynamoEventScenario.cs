﻿using System;
using System.Collections.Generic;
using System.Text;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using WorkflowCore.Tests.DynamoDB;
using Xunit;

namespace WorkflowCore.Tests.MongoDB.Scenarios
{
    [Collection("DynamoDb collection")]
    public class DynamoEventScenario : EventScenario
    {        
        protected override void ConfigureServices(IServiceCollection services)
        {
            var cfg = new AmazonDynamoDBConfig {ServiceURL = DynamoDbDockerSetup.ConnectionString};
            services.AddWorkflow(x => x.UseAwsDynamoPersistence(DynamoDbDockerSetup.Credentials, cfg, "tests-"));
        }
    }
}
