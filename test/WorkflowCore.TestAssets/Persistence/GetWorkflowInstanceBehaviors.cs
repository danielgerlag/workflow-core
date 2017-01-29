using Machine.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;

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
            Utils.CompareObjects(workflow, retrievedWorkflow).ShouldBeTrue();
        };

    }
}
