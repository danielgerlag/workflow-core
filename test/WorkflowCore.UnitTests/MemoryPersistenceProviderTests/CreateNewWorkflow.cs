using Machine.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Services;
using WorkflowCore.TestAssets;

namespace WorkflowCore.UnitTests.MemoryPersistenceProviderTests
{    
    [Subject(typeof(MemoryPersistenceProvider))]
    public class CreateNewWorkflow
    {
        Establish context = () =>
        {
            Subject = new MemoryPersistenceProvider();
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
                Active = true,
                StepId = 0
            });
        };

        Because of = () => workflowId = Subject.CreateNewWorkflow(workflow).Result;

        It should_return_a_generated_id = () => workflowId.ShouldNotBeNull();
        It should_set_id_on_object = () => workflow.Id.ShouldNotBeNull();

        Cleanup after = () =>
        {            
        };

        static IPersistenceProvider Subject;
        static WorkflowInstance workflow;
        static string workflowId;


    }
}
