using Machine.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.PostgreSQL;
using WorkflowCore.Services;
using WorkflowCore.TestAssets;

namespace WorkflowCore.Tests.PostgreSQL.PersistenceProviderTests
{    
    [Subject(typeof(PostgresPersistenceProvider))]    
    public class PersistWorkflow
    {
        Establish context = () =>
        {
            Subject = new PostgresPersistenceProvider("Server=127.0.0.1;Port=" + DockerSetup.Port + ";Database=workflow;User Id=postgres;", true, true);
            Subject.EnsureStoreExists();
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
                Active = true,
                StepId = 0
            });

            workflowId = Subject.CreateNewWorkflow(oldWorkflow).Result;

            newWorkflow = Utils.DeepCopy(oldWorkflow);
            newWorkflow.NextExecution = 7;
            newWorkflow.ExecutionPointers.Add(new ExecutionPointer() { Active = true, StepId = 1 });
        };

        Because of = () => Subject.PersistWorkflow(newWorkflow).Wait();

        It should_store_the_difference = () =>
        {
            var oldWorkflow = Subject.GetWorkflowInstance(workflowId).Result;
            Utils.CompareObjects(oldWorkflow, newWorkflow).ShouldBeTrue();
        };

        Cleanup after = () =>
        {
            Subject = null;
            newWorkflow = null;            
            workflowId = null;
        };

        static IPersistenceProvider Subject;        
        static WorkflowInstance newWorkflow;
        static string workflowId;


    }
}
