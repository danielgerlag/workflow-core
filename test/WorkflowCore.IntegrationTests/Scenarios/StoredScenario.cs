﻿using System;
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
        public void should_execute_json_workflow()
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
        
        [Fact]
        public void should_execute_json_workflow_with_dynamic_data()
        {
            var initialData = new DynamicData
            {
                ["Flag1"] = true,
                ["Flag2"] = true,
                ["Counter1"] = 0,
                ["Counter2"] = 0,
                ["Counter3"] = 0,
                ["Counter4"] = 0,
                ["Counter5"] = 0,
                ["Counter6"] = 0
            };

            var workflowId = StartWorkflow(TestAssets.Utils.GetTestDefinitionDynamicJson(), initialData);
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            var data = GetData<DynamicData>(workflowId);
            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
            data["Counter1"].Should().Be(1);
            data["Counter2"].Should().Be(1);
            data["Counter3"].Should().Be(1);
            data["Counter4"].Should().Be(1);
            data["Counter5"].Should().Be(0);
            data["Counter6"].Should().Be(1);
        }
    }
}
