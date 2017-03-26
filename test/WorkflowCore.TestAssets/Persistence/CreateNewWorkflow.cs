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
        
    public abstract class CreateNewWorkflow
    {
        protected static IPersistenceProvider Subject;
        protected static WorkflowInstance workflow;
        protected static string workflowId;

        protected abstract IPersistenceProvider Provider { get; }
        Establish context;

        public CreateNewWorkflow()
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
                WorkflowDefinitionId = "My Workflow"
            };
            workflow.ExecutionPointers.Add(new ExecutionPointer()
            {
                Id = Guid.NewGuid().ToString(),
                Active = true,
                StepId = 0
            });
        }

        Because of = () => workflowId = Subject.CreateNewWorkflow(workflow).Result;        
    }
}
