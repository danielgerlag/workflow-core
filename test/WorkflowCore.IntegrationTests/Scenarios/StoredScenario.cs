using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;
using FluentAssertions;
using WorkflowCore.Testing;
using WorkflowCore.TestAssets.DataTypes;

namespace WorkflowCore.IntegrationTests.Scenarios
{
    public class StoredScenario : JsonWorkflowTest
    {   
        public StoredScenario()
        {
            Setup();
        }

        [Fact(DisplayName = "Execute workflow from stored JSON definition")]
        public void Scenario()
        {
            var workflowId = StartWorkflow(TestAssets.Utils.GetTestDefinitionJson(), new CounterBoard() { Flag1 = true, Flag2 = true });
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            var data = GetData<CounterBoard>(workflowId);
            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
            data.Counter1.Should().Be(1);
            data.Counter2.Should().Be(1);
            data.Counter3.Should().Be(1);
            data.Counter4.Should().Be(1);
            data.Counter5.Should().Be(0);
            data.Counter6.Should().Be(1);
        }
    }
}
