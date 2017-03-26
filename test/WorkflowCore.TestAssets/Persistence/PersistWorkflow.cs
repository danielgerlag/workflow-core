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

    public abstract class PersistWorkflow
    {
        protected static IPersistenceProvider Subject;
        protected static WorkflowInstance newWorkflow;
        protected static string workflowId;

        protected abstract IPersistenceProvider Provider { get; }
        Establish context;

        public PersistWorkflow()
        {
            context = EstablishContext;
        }

        protected void EstablishContext()
        {
            Subject = Provider;

            var oldWorkflow = new WorkflowInstance()
            {
                Data = new { Value1 = 7 },
                Description = "My Description",
                Status = WorkflowStatus.Runnable,
                NextExecution = 0,
                Version = 1,
                WorkflowDefinitionId = "My Workflow",
                CreateTime = new DateTime(2000, 1, 1).ToUniversalTime()
            };
            oldWorkflow.ExecutionPointers.Add(new ExecutionPointer()
            {
                Id = Guid.NewGuid().ToString(),
                Active = true,
                StepId = 0
            });

            workflowId = Subject.CreateNewWorkflow(oldWorkflow).Result;

            newWorkflow = Utils.DeepCopy(oldWorkflow);
            newWorkflow.NextExecution = 7;
            newWorkflow.ExecutionPointers.Add(new ExecutionPointer() { Id = Guid.NewGuid().ToString(), Active = true, StepId = 1 });
        }

        Because of = () => Subject.PersistWorkflow(newWorkflow).Wait();
                
        Cleanup after = () =>
        {
            Subject = null;
            newWorkflow = null;
            workflowId = null;
        };
    }
}
