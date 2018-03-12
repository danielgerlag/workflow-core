﻿using FakeItEasy;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services.DefinitionStorage;
using WorkflowCore.TestAssets.DataTypes;
using WorkflowCore.TestAssets.Steps;
using Xunit;

namespace WorkflowCore.UnitTests.Services.DefinitionStorage
{
    public class DefinitionLoaderTests
    {

        private readonly IDefinitionLoader _subject;
        private readonly IWorkflowRegistry _registry;

        public DefinitionLoaderTests()
        {
            _registry = A.Fake<IWorkflowRegistry>();
            _subject = new DefinitionLoader(_registry, null);
        }

        [Fact(DisplayName = "Should register workflow")]
        public void RegisterDefintion()
        {
            _subject.LoadDefinition("{\"Id\": \"HelloWorld\", \"Version\": 1, \"Steps\": []}");

            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.Id == "HelloWorld"))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.Version == 1))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.DataType == typeof(object)))).MustHaveHappened();
        }

        [Fact(DisplayName = "Should parse definition")]
        public void ParseDefintion()
        {
            _subject.LoadDefinition(TestAssets.Utils.GetTestDefinitionJson());

            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.Id == "Test"))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.Version == 1))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.DataType == typeof(TestAssets.DataTypes.CounterBoard)))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(MatchTestDefinition, ""))).MustHaveHappened();
        }


        private bool MatchTestDefinition(WorkflowDefinition def)
        {
            //TODO: make this better
            var step1 = def.Steps.Single(s => s.Tag == "Step1");
            var step2 = def.Steps.Single(s => s.Tag == "Step2");
            
            step1.Outcomes.Count.Should().Be(1);
            step1.Inputs.Count.Should().Be(1);
            step1.Outputs.Count.Should().Be(1);
            step1.Outcomes.Single().NextStep.Should().Be(step2.Id);

            return true;
        }

    }
}
