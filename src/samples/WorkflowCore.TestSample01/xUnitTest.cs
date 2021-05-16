using FluentAssertions;
using System;
using WorkflowCore.Models;
using WorkflowCore.Testing;
using WorkflowCore.TestSample01.Workflow;
using Xunit;

namespace WorkflowCore.TestSample01
{
    public class xUnitTest : WorkflowTest<MyWorkflow, MyDataClass>
    {
        public xUnitTest()
        {
            Setup();
        }

        [Fact]
        public void MyWorkflow()
        {
            var workflowId = StartWorkflow(new MyDataClass { Value1 = 2, Value2 = 3 });
            WaitForWorkflowToComplete(workflowId, TimeSpan.FromSeconds(30));

            GetStatus(workflowId).Should().Be(WorkflowStatus.Complete);
            UnhandledStepErrors.Count.Should().Be(0);
            GetData(workflowId).Value3.Should().Be(5);
        }
    }
}
