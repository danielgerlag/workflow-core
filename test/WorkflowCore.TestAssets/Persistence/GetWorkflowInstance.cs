using Machine.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using FluentAssertions;

namespace WorkflowCore.TestAssets.Persistence
{
    [Behaviors]
    public class GetWorkflowInstanceBehaviors
    {
        protected static IPersistenceProvider Subject;
        protected static WorkflowInstance workflow;
        protected static WorkflowInstance retrievedWorkflow;
        protected static string workflowId;

        It should_match_the_original = () =>
        {
            retrievedWorkflow.ShouldBeEquivalentTo(workflow);
        };
    }

    public abstract class GetWorkflowInstance
    {
        protected static IPersistenceProvider Subject;
        protected static WorkflowInstance workflow;
        protected static string workflowId;
        protected static WorkflowInstance retrievedWorkflow;

        protected abstract IPersistenceProvider Provider { get; }
        Establish context;

        public GetWorkflowInstance()
        {
            context = EstablishContext;
        }

        protected void EstablishContext()
        {
            Subject = Provider;
            workflow = new WorkflowInstance()
            {
                Data = new { Value1 = 7 },
                Description = "My Description",
                Status = WorkflowStatus.Runnable,
                NextExecution = 0,
                Version = 1,
                WorkflowDefinitionId = "My Workflow",
                CreateTime = new DateTime(2000, 1, 1).ToUniversalTime()
            };

            var ep = new ExecutionPointer()
            {
                Id = Guid.NewGuid().ToString(),
                Active = true,
                StepId = 0
            };

            ep.ExtensionAttributes["Attr1"] = "test";
            ep.ExtensionAttributes["Attr2"] = 5;
            workflow.ExecutionPointers.Add(ep);

            workflowId = Subject.CreateNewWorkflow(workflow).Result;
        }

        Because of = () => retrievedWorkflow = Subject.GetWorkflowInstance(workflowId).Result;

        Cleanup after = () =>
        {
            Subject = null;
            workflow = null;
            retrievedWorkflow = null;
            workflowId = null;
        };

    }
}
