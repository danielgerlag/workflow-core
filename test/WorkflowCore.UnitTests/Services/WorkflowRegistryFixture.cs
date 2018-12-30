using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;
using FluentAssertions;
using Xunit;
using WorkflowCore.Primitives;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace WorkflowCore.UnitTests.Services
{
    public class WorkflowRegistryFixture
    {
        protected IServiceProvider ServiceProvider { get; }
        protected WorkflowRegistry Subject { get; }
        protected WorkflowDefinition Definition { get; }

        public WorkflowRegistryFixture()
        {
            ServiceProvider = A.Fake<IServiceProvider>();
            Subject = new WorkflowRegistry(ServiceProvider);

            Definition = new WorkflowDefinition{
                Id = "TestWorkflow",
                Version = 1,
            };
            Subject.RegisterWorkflow(Definition);
        }

        [Fact(DisplayName = "Should return existing workflow")]
        public void getdefinition_should_return_existing_workflow()
        {
            Subject.GetDefinition(Definition.Id).Should().Be(Definition);
            Subject.GetDefinition(Definition.Id, Definition.Version).Should().Be(Definition);
        }

        [Fact(DisplayName = "Should return null on unknown workflow")]
        public void getdefinition_should_return_null_on_unknown()
        {
            Subject.GetDefinition("UnkownWorkflow").Should().BeNull();
            Subject.GetDefinition("UnkownWorkflow", 1).Should().BeNull();
        }

        [Fact(DisplayName = "Should return highest version of existing workflow")]
        public void getdefinition_should_return_highest_version_workflow()
        {
            var definition2 = new WorkflowDefinition{
                Id = Definition.Id,
                Version = Definition.Version + 1,
            };
            Subject.RegisterWorkflow(definition2);

            Subject.GetDefinition(Definition.Id).Should().Be(definition2);
            Subject.GetDefinition(Definition.Id, definition2.Version).Should().Be(definition2);
            Subject.GetDefinition(Definition.Id, Definition.Version).Should().Be(Definition);
        }
    }
}