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
    public class CreateNewWorkflowBehaviors
    {
        protected static IPersistenceProvider Subject;
        protected static WorkflowInstance workflow;
        protected static string workflowId;

        It should_return_a_generated_id = () => workflowId.ShouldNotBeNull();
        It should_set_id_on_object = () => workflow.Id.ShouldNotBeNull();
    }
}
