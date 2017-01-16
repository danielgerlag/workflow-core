using Machine.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.PostgreSQL;

namespace WorkflowCore.Tests.PostgreSQL.PersistenceProviderTests
{
    [Subject(typeof(PostgresPersistenceProvider))]
    public class CreateNewWorkflow
    {
        Establish context = () =>
        {
            Subject = new PostgresPersistenceProvider("Server=127.0.0.1;Port=" + DockerSetup.Port + ";Database=workflow;User Id=postgres;", true, true);
            Subject.EnsureStoreExists();
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
