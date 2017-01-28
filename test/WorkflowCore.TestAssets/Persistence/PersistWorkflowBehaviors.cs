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
    public class PersistWorkflowBehaviors
    {
        protected static IPersistenceProvider Subject;
        protected static WorkflowInstance newWorkflow;
        protected static string workflowId;


        It should_store_the_difference = () =>
        {
            var oldWorkflow = Subject.GetWorkflowInstance(workflowId).Result;
            Utils.CompareObjects(oldWorkflow, newWorkflow).ShouldBeTrue();
        };

    }
}
