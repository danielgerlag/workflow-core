using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using WorkflowCore.Models;
using WorkflowCore.Testing;
using WorkflowCore.TestSample01.Workflow;

namespace WorkflowCore.TestSample01
{
    [TestFixture]
    public class NUnitTest : WorkflowTest<MyWorkflow, MyDataClass>
    {
        [SetUp]
        protected override void Setup(bool registerClassMap = false)
        {
            base.Setup(registerClassMap);
        }

        [Test]
        public void NUnit_workflow_test_sample()
        {
            var workflowId = StartWorkflow(new MyDataClass() { Value1 = 2, Value2 = 3 });
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
            GetData(workflowId).Value3.Should().Be(5);
        }

    }
}
