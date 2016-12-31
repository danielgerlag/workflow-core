using Machine.Specifications;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Persistence.MongoDB.Services;

namespace WorkflowCore.Tests.MongoDB.MongoPersistenceProviderTests
{
    [Subject(typeof(MongoPersistenceProvider))]
    public class CreateNewWorkflow
    {
        Establish context = () =>
        {
            var client = new MongoClient("mongodb://localhost:28017");
            var db = client.GetDatabase("workflow-tests");

            Subject = new MongoPersistenceProvider(db);
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
