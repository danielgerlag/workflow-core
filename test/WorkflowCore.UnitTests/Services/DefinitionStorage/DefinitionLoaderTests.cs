using FakeItEasy;
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
            _subject = new DefinitionLoader(_registry);
        }

        [Fact(DisplayName = "Should register workflow")]
        public void RegisterDefinition()
        {
            _subject.LoadDefinition("{\"Id\": \"HelloWorld\", \"Version\": 1, \"Steps\": []}", Deserializers.Json);

            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.Id == "HelloWorld"))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.Version == 1))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.DataType == typeof(object)))).MustHaveHappened();
        }

        [Fact(DisplayName = "Should parse definition")]
        public void ParseDefinition()
        {
            _subject.LoadDefinition(TestAssets.Utils.GetTestDefinitionJson(), Deserializers.Json);

            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.Id == "Test"))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.Version == 1))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.DataType == typeof(CounterBoard)))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(MatchTestDefinition, ""))).MustHaveHappened();
        }


        [Fact(DisplayName = "Should parse definition")]
        public void ParseDefinitionDynamic()
        {
            _subject.LoadDefinition(TestAssets.Utils.GetTestDefinitionDynamicJson(), Deserializers.Json);

            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.Id == "Test"))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.Version == 1))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.DataType == typeof(DynamicData)))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(MatchTestDefinition, ""))).MustHaveHappened();
        }

        [Fact(DisplayName = "Should throw error for bad input property name on step")]
        public void ParseDefinitionInputException()
        {
            Assert.Throws<ArgumentException>(() => _subject.LoadDefinition(TestAssets.Utils.GetTestDefinitionJsonMissingInputProperty(), Deserializers.Json));
        }

        private bool MatchTestDefinition(WorkflowDefinition def)
        {
            //TODO: make this better
            var step1 = def.Steps.Single(s => s.ExternalId == "Step1");
            var step2 = def.Steps.Single(s => s.ExternalId == "Step2");

            step1.Outcomes.Count.Should().Be(1);
            step1.Inputs.Count.Should().Be(1);
            step1.Outputs.Count.Should().Be(1);
            step1.Outcomes.Single().NextStep.Should().Be(step2.Id);

            return true;
        }

    }
}
