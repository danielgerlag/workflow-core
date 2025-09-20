using System;
using System.Collections.Generic;
using WorkflowCore.Interface;
using WorkflowCore.Services.DefinitionStorage;
using Xunit;

namespace WorkflowCore.UnitTests.Services.DefinitionStorage
{
    /// <summary>
    /// Integration test to verify the fix for inherited property binding in YAML definitions.
    /// This test specifically reproduces the issue mentioned in GitHub issue #1375.
    /// </summary>
    public class YamlInheritedPropertyIntegrationTest
    {
        [Fact(DisplayName = "Should bind inherited properties like RunParallel from base Foreach class")]
        public void ShouldBindInheritedPropertiesInYamlDefinition()
        {
            // Arrange
            var registry = new WorkflowRegistry();
            var loader = new DefinitionLoader(registry, new TypeResolver());

            // This YAML definition uses a custom step (IterateListStep) that inherits from Foreach
            // and tries to set the RunParallel property which is defined in the base Foreach class
            var yamlWithInheritedProperty = @"
Id: TestInheritedPropertyWorkflow
Version: 1
Description: Test workflow for inherited property binding
DataType: WorkflowCore.TestAssets.DataTypes.CounterBoard, WorkflowCore.TestAssets
Steps:
  - Id: IterateList
    StepType: WorkflowCore.TestAssets.Steps.IterateListStep, WorkflowCore.TestAssets
    Inputs:
      Collection: ""data.DataList""
      RunParallel: false
";

            // Act & Assert
            // Before the fix, this would throw: "Unknown property for input RunParallel on IterateList"
            // After the fix, this should succeed
            var exception = Record.Exception(() => 
                loader.LoadDefinition(yamlWithInheritedProperty, Deserializers.Yaml));

            // Verify no exception was thrown
            Assert.Null(exception);
        }

        [Fact(DisplayName = "Should still throw exception for truly unknown properties")]
        public void ShouldStillThrowForUnknownProperties()
        {
            // Arrange
            var registry = new WorkflowRegistry();
            var loader = new DefinitionLoader(registry, new TypeResolver());

            var yamlWithUnknownProperty = @"
Id: TestInheritedPropertyWorkflow
Version: 1
Description: Test workflow for inherited property binding
DataType: WorkflowCore.TestAssets.DataTypes.CounterBoard, WorkflowCore.TestAssets
Steps:
  - Id: IterateList
    StepType: WorkflowCore.TestAssets.Steps.IterateListStep, WorkflowCore.TestAssets
    Inputs:
      Collection: ""data.DataList""
      NonExistentProperty: false
";

            // Act & Assert
            // This should still throw an exception for truly unknown properties
            var exception = Assert.Throws<ArgumentException>(() => 
                loader.LoadDefinition(yamlWithUnknownProperty, Deserializers.Yaml));
            
            Assert.Contains("Unknown property for input NonExistentProperty", exception.Message);
        }
    }
}