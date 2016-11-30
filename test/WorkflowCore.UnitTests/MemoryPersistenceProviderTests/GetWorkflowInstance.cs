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
    public class GetWorkflowInstance
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

            workflowId = Subject.CreateNewWorkflow(workflow).Result;
        };

        Because of = () => retrievedWorkflow = Subject.GetWorkflowInstance(workflowId).Result;

        It should_match_the_original = () => Utils.CompareObjects(workflow, retrievedWorkflow).ShouldBeTrue();        

        Cleanup after = () =>
        {
            Subject = null;
            workflow = null;
            retrievedWorkflow = null;
            workflowId = null;
        };

        static IPersistenceProvider Subject;
        static WorkflowInstance workflow;
        static WorkflowInstance retrievedWorkflow;
        static string workflowId;


    }
}
