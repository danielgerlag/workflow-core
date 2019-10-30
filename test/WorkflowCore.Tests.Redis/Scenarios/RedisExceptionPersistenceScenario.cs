using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using Xunit;

namespace WorkflowCore.Tests.Redis.Scenarios
{
    [Collection("Redis collection")]
    public class RedisExceptionPersistenceScenario : ExceptionPersistenceScenario
    {        
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UseRedisPersistence(RedisDockerSetup.ConnectionString, "scenario-"));
        }
    }
}