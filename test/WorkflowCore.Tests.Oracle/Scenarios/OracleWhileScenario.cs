﻿using System;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.IntegrationTests.Scenarios;
using WorkflowCore.Persistence.Oracle;

using Xunit;

namespace WorkflowCore.Tests.Oracle.Scenarios
{
    [Collection("Oracle collection")]
    public class OracleWhileScenario : WhileScenario<OracleWhileScenario>
    {        
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkflow(x => x.UseOracle(OracleDockerSetup.ConnectionString, true, true));
        }
    }
}
