using FakeItEasy;
using FluentAssertions;
using Newtonsoft.Json;
using System;
using System.Linq;
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
            _subject = new DefinitionLoader(_registry, new TypeResolver());
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
        public void ParseDefinitionPropertyDynamic()
        {
            _subject.LoadDefinition(TestAssets.Utils.GetTestDefinitionDynamicYaml(), Deserializers.Yaml);

            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.Id == "Test"))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.Version == 1))).MustHaveHappened();
            A.CallTo(() => _registry.RegisterWorkflow(A<WorkflowDefinition>.That.Matches(x => x.DataType == typeof(CounterBoardWithDynamicData)))).MustHaveHappened();
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

        // Regression test for issue #1428: a scalar variable-binding input plus a
        // scalar string-literal input. The compiled input expressions used to be
        // built inside a closure and recompiled on every invocation, which produced
        // an InvalidProgramException on .NET 10. Loading the definition and then
        // assigning the inputs (as WorkflowExecutor.ExecuteStep does) must succeed
        // and resolve both values.
        [Fact(DisplayName = "Should evaluate scalar variable and string-literal inputs")]
        public void ParseAndAssignScalarInputs()
        {
            var dataType = typeof(ScalarInputData).AssemblyQualifiedName;
            var stepType = typeof(ScalarInputStep).AssemblyQualifiedName;

            var json =
                "{" +
                "\"Id\": \"Issue1428\", \"Version\": 1," +
                "\"DataType\": " + JsonConvert.ToString(dataType) + "," +
                "\"Steps\": [{" +
                    "\"Id\": \"UpdateStatus\"," +
                    "\"Name\": \"Update internal status\"," +
                    "\"StepType\": " + JsonConvert.ToString(stepType) + "," +
                    "\"Inputs\": {" +
                        "\"MessageId\": \"data.MessageId\"," +
                        "\"Status\": \"\\\"waits-for-batching\\\"\"" +
                    "}" +
                "}]}";

            var def = _subject.LoadDefinition(json, Deserializers.Json);

            var step = def.Steps.Single(s => s.ExternalId == "UpdateStatus");
            step.Inputs.Count.Should().Be(2);

            var body = new ScalarInputStep();
            var data = new ScalarInputData { MessageId = "msg-42" };

            foreach (var input in step.Inputs)
                input.AssignInput(data, body, null);

            body.MessageId.Should().Be("msg-42");
            body.Status.Should().Be("waits-for-batching");
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
