using System;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.AI.AzureFoundry.Interface;
using WorkflowCore.AI.AzureFoundry.Models;
using WorkflowCore.AI.AzureFoundry.Services;
using Xunit;
using FluentAssertions;

namespace WorkflowCore.AI.AzureFoundry.Tests
{
    public class ToolRegistryTests
    {
        [Fact]
        public void Register_ShouldAddToolToRegistry()
        {
            // Arrange
            var registry = new ToolRegistry(null);
            var tool = new TestTool();

            // Act
            registry.Register(tool);

            // Assert
            registry.HasTool("test_tool").Should().BeTrue();
            registry.GetTool("test_tool").Should().Be(tool);
        }

        [Fact]
        public void GetTool_ShouldReturnNullForUnregisteredTool()
        {
            // Arrange
            var registry = new ToolRegistry(null);

            // Act
            var tool = registry.GetTool("nonexistent");

            // Assert
            tool.Should().BeNull();
        }

        [Fact]
        public void GetAllTools_ShouldReturnAllRegisteredTools()
        {
            // Arrange
            var registry = new ToolRegistry(null);
            registry.Register(new TestTool());
            registry.Register(new AnotherTestTool());

            // Act
            var tools = registry.GetAllTools();

            // Assert
            tools.Should().HaveCount(2);
        }

        [Fact]
        public void GetToolDefinitions_ShouldReturnDefinitionsForAllTools()
        {
            // Arrange
            var registry = new ToolRegistry(null);
            registry.Register(new TestTool());

            // Act
            var definitions = registry.GetToolDefinitions();

            // Assert
            definitions.Should().ContainSingle(d => 
                d.Name == "test_tool" && 
                d.Description == "A test tool");
        }

        private class TestTool : IAgentTool
        {
            public string Name => "test_tool";
            public string Description => "A test tool";
            public string ParametersSchema => "{}";

            public Task<ToolResult> ExecuteAsync(string toolCallId, string arguments, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(ToolResult.Succeeded(toolCallId, Name, "test result"));
            }
        }

        private class AnotherTestTool : IAgentTool
        {
            public string Name => "another_tool";
            public string Description => "Another test tool";
            public string ParametersSchema => "{}";

            public Task<ToolResult> ExecuteAsync(string toolCallId, string arguments, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(ToolResult.Succeeded(toolCallId, Name, "another result"));
            }
        }
    }
}
